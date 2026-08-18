import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { mensagemDeErro } from '../nucleo/api';
import { Plano } from '../planos/plano';
import { PlanoServico } from '../planos/plano-servico';
import {
    Beneficiario,
    StatusBeneficiario
} from './beneficiario';
import { BeneficiarioServico } from './beneficiario-servico';
import { BeneficiarioFormulario } from './form/beneficiario-formulario';

@Component({
    selector: 'app-beneficiarios-lista',
    imports: [BeneficiarioFormulario],
    templateUrl: './beneficiarios-lista.html',
    styleUrl: './beneficiarios-lista.css',
})
export class BeneficiariosLista {
    private readonly servico = inject(BeneficiarioServico);
    private readonly planoServico = inject(PlanoServico);
    private readonly destroyRef = inject(DestroyRef);

    protected readonly beneficiarios = signal<Beneficiario[]>([]);
    protected readonly planos = signal<Plano[]>([]);

    protected readonly beneficiarioEmEdicao =
        signal<Beneficiario | null>(null);

    protected readonly excluindoId = signal<string | null>(null);

    protected readonly carregando = signal(true);
    protected readonly erro = signal<string | null>(null);

    protected readonly pagina = signal(1);
    protected readonly tamanho = signal(10);
    protected readonly total = signal(0);

    protected readonly filtroStatus = signal<StatusBeneficiario | ''>('');
    protected readonly filtroPlanoId = signal('');

    constructor() {
        this.carregarPlanos();
        this.carregar();
    }

    protected carregar(): void {
        this.carregando.set(true);
        this.erro.set(null);

        this.servico
            .listar({
                pagina: this.pagina(),
                tamanho: this.tamanho(),
                status: this.filtroStatus() || undefined,
                plano_id: this.filtroPlanoId() || undefined
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (resposta) => {
                    this.beneficiarios.set(resposta.dados);
                    this.pagina.set(resposta.pagina);
                    this.tamanho.set(resposta.tamanho);
                    this.total.set(resposta.total);
                    this.carregando.set(false);
                },
                error: (resposta: HttpErrorResponse) => {
                    this.erro.set(mensagemDeErro(resposta));
                    this.carregando.set(false);
                }
            });
    }

    protected editar(beneficiario: Beneficiario): void {
        this.beneficiarioEmEdicao.set(beneficiario);
    }

    protected cancelarEdicao(): void {
        this.beneficiarioEmEdicao.set(null);
    }

    protected depoisDeAtualizar(): void {
        this.beneficiarioEmEdicao.set(null);
        this.carregar();
    }

    protected alterarStatus(evento: Event): void {
        const select = evento.target as HTMLSelectElement;

        this.filtroStatus.set(
            select.value as StatusBeneficiario | ''
        );

        this.pagina.set(1);
        this.carregar();
    }

    protected excluir(beneficiario: Beneficiario): void {
        const confirmou = window.confirm(
            `Deseja excluir o beneficiário ${beneficiario.nome_completo}?`
        );

        if (!confirmou) {
            return;
        }

        this.erro.set(null);
        this.excluindoId.set(beneficiario.id);

        this.servico
            .excluir(beneficiario.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.excluindoId.set(null);

                    if (
                        this.beneficiarios().length === 1
                        && this.pagina() > 1
                    ) {
                        this.pagina.update(
                            (paginaAtual) => paginaAtual - 1
                        );
                    }

                    this.carregar();
                },
                error: (resposta: HttpErrorResponse) => {
                    this.erro.set(mensagemDeErro(resposta));
                    this.excluindoId.set(null);
                }
            });
    }
    protected alterarPlano(evento: Event): void {
        const select = evento.target as HTMLSelectElement;

        this.filtroPlanoId.set(select.value);

        this.pagina.set(1);
        this.carregar();
    }

    protected paginaAnterior(): void {
        if (this.pagina() <= 1) {
            return;
        }

        this.pagina.update((paginaAtual) => paginaAtual - 1);
        this.carregar();
    }

    protected alterarTamanho(evento: Event): void {
        const select = evento.target as HTMLSelectElement;

        this.tamanho.set(Number(select.value));
        this.pagina.set(1);
        this.carregar();
    }

    protected proximaPagina(): void {
        if (!this.temProximaPagina()) {
            return;
        }

        this.pagina.update((paginaAtual) => paginaAtual + 1);
        this.carregar();
    }

    protected temProximaPagina(): boolean {
        return this.pagina() * this.tamanho() < this.total();
    }

    protected nomeDoPlano(planoId: string): string {
        return (
            this.planos().find((plano) => plano.id === planoId)?.nome
            ?? 'Plano indisponível'
        );
    }

    private carregarPlanos(): void {
        this.planoServico
            .listar()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: (planos) => {
                    this.planos.set(planos);
                },
                error: (resposta: HttpErrorResponse) => {
                    this.erro.set(mensagemDeErro(resposta));
                }
            });
    }
}