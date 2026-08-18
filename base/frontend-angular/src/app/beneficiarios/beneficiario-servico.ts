import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_BASE } from '../nucleo/api';
import {
    AtualizarBeneficiario,
    Beneficiario,
    BeneficiariosPaginados,
    CriarBeneficiario,
    FiltrosBeneficiarios,
} from './beneficiario';


@Injectable({
    providedIn: 'root',
})
export class BeneficiarioServico {
    private readonly http = inject(HttpClient);
    private readonly apiBase = inject(API_BASE);

    listar(filtros: FiltrosBeneficiarios = {}): Observable<BeneficiariosPaginados> {
        let params = new HttpParams();

        if (filtros.pagina !== undefined) {
            params = params.set('pagina', filtros.pagina);
        }

        if (filtros.tamanho !== undefined) {
            params = params.set('tamanho', filtros.tamanho);
        }

        if (filtros.status) {
            params = params.set('status', filtros.status);
        }

        if (filtros.plano_id) {
            params = params.set('plano_id', filtros.plano_id);
        }

        return this.http.get<BeneficiariosPaginados>(
            `${this.apiBase}/beneficiarios`,
            { params },
        );
    }

    buscarPorId(id: string): Observable<Beneficiario> {
        return this.http.get<Beneficiario>(
            `${this.apiBase}/beneficiarios/${id}`,
        );
    }

    criar(dados: CriarBeneficiario): Observable<Beneficiario> {
        return this.http.post<Beneficiario>(
            `${this.apiBase}/beneficiarios`,
            dados,
        );
    }

    atualizar(
        id: string,
        dados: AtualizarBeneficiario,
    ): Observable<Beneficiario> {
        return this.http.put<Beneficiario>(
            `${this.apiBase}/beneficiarios/${id}`,
            dados,
        );
    }

    excluir(id: string): Observable<void> {
        return this.http.delete<void>(
            `${this.apiBase}/beneficiarios/${id}`,
        );
    }
}