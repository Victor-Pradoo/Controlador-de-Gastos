import { Pipe, PipeTransform } from '@angular/core';

/** Formata em BRL. Um so lugar decide como dinheiro aparece na tela. */
@Pipe({ name: 'brl' })
export class BrlPipe implements PipeTransform {
  private static readonly formatter = new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
  });

  transform(value: number | null | undefined): string {
    return BrlPipe.formatter.format(value ?? 0);
  }
}
