import { HttpErrorResponse } from '@angular/common/http';
import { Component, EventEmitter, inject, Input, OnChanges, Output, signal, SimpleChanges } from '@angular/core';

import { mensagemDeErro } from '../../nucleo/api';
import { Plano } from '../../planos/plano';
import { BeneficiarioServico } from '../beneficiario-servico';
import { Beneficiario, StatusBeneficiario } from '../beneficiario';

@Component({
    selector: 'app-beneficiario-formulario',
    templateUrl: './beneficiario-formulario.html',
    styleUrl: './beneficiario-formulario.css'
})
export class BeneficiarioFormulario implements OnChanges {

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['beneficiario']) {
            const beneficiario = this.beneficiario;

            if (beneficiario) {
                this.carregarBeneficiario(beneficiario);
            } else {
                this.limpar();
            }
        }
    }

    private readonly servico = inject(BeneficiarioServico);

    @Input({ required: true })
    planos: Plano[] = [];

    @Output()
    cadastrado = new EventEmitter<void>();

    @Input()
    beneficiario: Beneficiario | null = null;

    @Output()
    cancelado = new EventEmitter<void>();

    @Output()
    atualizado = new EventEmitter<void>();

    protected status: StatusBeneficiario = 'ATIVO';

    protected readonly salvando = signal(false);
    protected readonly erro = signal<string | null>(null);

    protected nomeCompleto = '';
    protected cpf = '';
    protected dataNascimento = '';
    protected planoId = '';

    protected carregarBeneficiario(beneficiario: Beneficiario): void {
        this.nomeCompleto = beneficiario.nome_completo;
        this.cpf = beneficiario.cpf;
        this.dataNascimento = beneficiario.data_nascimento;
        this.planoId = beneficiario.plano_id;
        this.status = beneficiario.status;
    }

    protected salvar(): void {
        this.erro.set(null);

        const erroValidacao = this.validar();

        if (erroValidacao) {
            this.erro.set(erroValidacao);
            return;
        }

        this.salvando.set(true);

        if (this.beneficiario) {
            this.atualizar();
            return;
        }

        this.servico
            .criar({
                nome_completo: this.nomeCompleto.trim(),
                cpf: this.cpf,
                data_nascimento: this.dataNascimento,
                plano_id: this.planoId
            })
            .subscribe({
                next: () => {
                    this.salvando.set(false);
                    this.limpar();
                    this.cadastrado.emit();
                },
                error: (resposta: HttpErrorResponse) => {
                    this.erro.set(mensagemDeErro(resposta));
                    this.salvando.set(false);
                }
            });
    }

    private atualizar(): void {
        if (!this.beneficiario) {
            return;
        }

        this.salvando.set(true);

        this.servico
            .atualizar(this.beneficiario.id, {
                nome_completo: this.nomeCompleto.trim(),
                data_nascimento: this.dataNascimento,
                plano_id: this.planoId,
                status: this.status
            })
            .subscribe({
                next: () => {
                    this.salvando.set(false);
                    this.atualizado.emit();
                },
                error: (resposta: HttpErrorResponse) => {
                    this.erro.set(mensagemDeErro(resposta));
                    this.salvando.set(false);
                }
            });
    }

    private validar(): string | null {
        if (!this.nomeCompleto.trim()) {
            return 'Informe o nome completo.';
        }

        if (!this.cpf) {
            return 'Informe o CPF.';
        }

        if (!this.cpfValido(this.cpf)) {
            return 'Informe um CPF válido com 11 dígitos, sem pontuação.';
        }

        if (!this.dataNascimento) {
            return 'Informe a data de nascimento.';
        }

        if (!this.dataNascimentoPassada(this.dataNascimento)) {
            return 'A data de nascimento precisa estar no passado.';
        }

        if (!this.planoId) {
            return 'Selecione um plano.';
        }

        return null;
    }

    private dataNascimentoPassada(data: string): boolean {
        const hoje = new Date();
        const nascimento = new Date(`${data}T00:00:00`);

        return nascimento < hoje;
    }

    private cpfValido(cpf: string): boolean {
        if (!/^\d{11}$/.test(cpf)) {
            return false;
        }

        if (/^(\d)\1{10}$/.test(cpf)) {
            return false;
        }

        const calcularDigito = (quantidade: number): number => {
            let soma = 0;

            for (let i = 0; i < quantidade; i++) {
                soma += Number(cpf[i]) * (quantidade + 1 - i);
            }

            const resto = soma % 11;

            return resto < 2 ? 0 : 11 - resto;
        };

        const primeiroDigito = calcularDigito(9);
        const segundoDigito = calcularDigito(10);

        return (
            primeiroDigito === Number(cpf[9])
            && segundoDigito === Number(cpf[10])
        );
    }

    private limpar(): void {
        this.nomeCompleto = '';
        this.cpf = '';
        this.dataNascimento = '';
        this.planoId = '';
        this.erro.set(null);
    }
}