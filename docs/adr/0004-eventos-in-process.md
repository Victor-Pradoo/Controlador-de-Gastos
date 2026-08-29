# ADR 0004 — Eventos de integração em processo

**Status:** aceito · **Data:** 2026-08-28

## Contexto

Módulos precisam se notificar (um lançamento registrado interessa a mais de um
módulo) sem se acoplar diretamente.

## Decisão

`IEventBus` no SharedKernel, implementado por `InMemoryEventBus`: entrega síncrona,
em processo, resolvendo handlers do container.

## Alternativas consideradas

- **Broker (RabbitMQ, Azure Service Bus) já no MVP.** Infraestrutura para operar,
  monitorar e pagar, para um app de um usuário só. Prematuro.
- **Chamadas diretas em tudo.** Funciona para "preciso da resposta agora"
  (`I*ModuleApi`), mas transforma notificação em acoplamento — o Ledger passaria a
  conhecer todo mundo que se interessa por seus eventos.

## Consequências

**A favor:** simples, testável, sem infraestrutura. Publicar e consumir eventos já é
o hábito do código, então a mudança futura é de implementação, não de desenho.

**Contra:** a entrega **não sobrevive a uma falha do processo** — se o handler quebra
depois do commit, o evento se perde. Aceitável enquanto nada crítico depende dele.
Quando deixar de ser: outbox transacional + broker, mantendo a mesma interface.
