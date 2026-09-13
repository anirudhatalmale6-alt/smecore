/* Comportamentos da interface. Tudo progressivo: sem JavaScript a aplicação continua a
   funcionar, apenas com um recarregamento de página em vez de uma atualização parcial. */
(function () {
    'use strict';

    /* --- Menu lateral em ecrãs pequenos ---------------------------------- */
    function ligarMenu() {
        var botao = document.querySelector('[data-acao="menu"]');
        if (!botao) return;

        botao.addEventListener('click', function () {
            var aberto = document.body.getAttribute('data-menu') === 'aberto';
            document.body.setAttribute('data-menu', aberto ? 'fechado' : 'aberto');
            botao.setAttribute('aria-expanded', aberto ? 'false' : 'true');
        });

        // Clicar fora fecha o menu. O ::after do body é apenas decorativo, por isso a área
        // clicável é o próprio documento, excluindo a barra lateral e o botão.
        document.addEventListener('click', function (evento) {
            if (document.body.getAttribute('data-menu') !== 'aberto') return;
            if (evento.target.closest('.lateral') || evento.target.closest('[data-acao="menu"]')) return;
            document.body.setAttribute('data-menu', 'fechado');
            botao.setAttribute('aria-expanded', 'false');
        });

        document.addEventListener('keydown', function (evento) {
            if (evento.key === 'Escape') {
                document.body.setAttribute('data-menu', 'fechado');
            }
        });
    }

    /* --- Confirmação antes de operações destrutivas ---------------------- */
    function ligarConfirmacoes(raiz) {
        (raiz || document).querySelectorAll('form[data-confirmar]').forEach(function (formulario) {
            if (formulario.dataset.confirmarLigado === '1') return;
            formulario.dataset.confirmarLigado = '1';

            formulario.addEventListener('submit', function (evento) {
                if (!window.confirm(formulario.getAttribute('data-confirmar'))) {
                    evento.preventDefault();
                }
            });
        });
    }

    /* --- Filtros que se aplicam sozinhos -------------------------------- */
    function ligarFiltrosAutomaticos(raiz) {
        (raiz || document).querySelectorAll('form[data-auto-submeter] select, form[data-auto-submeter] input[type="checkbox"]')
            .forEach(function (campo) {
                if (campo.dataset.autoLigado === '1') return;
                campo.dataset.autoLigado = '1';

                campo.addEventListener('change', function () {
                    var formulario = campo.closest('form');
                    // Uma mudança de filtro volta sempre à primeira página: manter a página
                    // atual mostraria uma tabela vazia sempre que o filtro reduz os resultados.
                    var pagina = formulario.querySelector('input[name="pagina"]');
                    if (pagina) pagina.value = '1';
                    formulario.requestSubmit ? formulario.requestSubmit() : formulario.submit();
                });
            });
    }

    /* --- Pesquisa com atraso (evita um pedido por cada tecla) ----------- */
    function ligarPesquisaComAtraso(raiz) {
        (raiz || document).querySelectorAll('input[data-pesquisa-atraso]').forEach(function (campo) {
            if (campo.dataset.atrasoLigado === '1') return;
            campo.dataset.atrasoLigado = '1';

            var temporizador = null;
            campo.addEventListener('input', function () {
                window.clearTimeout(temporizador);
                temporizador = window.setTimeout(function () {
                    var formulario = campo.closest('form');
                    if (!formulario) return;
                    var pagina = formulario.querySelector('input[name="pagina"]');
                    if (pagina) pagina.value = '1';
                    formulario.requestSubmit ? formulario.requestSubmit() : formulario.submit();
                }, parseInt(campo.getAttribute('data-pesquisa-atraso'), 10) || 450);
            });
        });
    }

    /* --- Normalização de matrícula ao escrever -------------------------- */
    function ligarMatriculas(raiz) {
        (raiz || document).querySelectorAll('input[data-matricula]').forEach(function (campo) {
            if (campo.dataset.matriculaLigada === '1') return;
            campo.dataset.matriculaLigada = '1';

            campo.addEventListener('blur', function () {
                var limpo = campo.value.replace(/[^A-Za-z0-9]/g, '').toUpperCase();
                if (limpo.length === 6) {
                    campo.value = limpo.slice(0, 2) + '-' + limpo.slice(2, 4) + '-' + limpo.slice(4, 6);
                }
            });
        });
    }

    function ligarTudo(raiz) {
        ligarConfirmacoes(raiz);
        ligarFiltrosAutomaticos(raiz);
        ligarPesquisaComAtraso(raiz);
        ligarMatriculas(raiz);
    }

    document.addEventListener('DOMContentLoaded', function () {
        ligarMenu();
        ligarTudo(document);
    });

    // Depois de uma atualização parcial do HTMX, os elementos novos precisam dos mesmos
    // comportamentos: sem isto, a segunda página de uma tabela perdia as confirmações.
    document.addEventListener('htmx:afterSwap', function (evento) {
        ligarTudo(evento.target);
    });
})();
