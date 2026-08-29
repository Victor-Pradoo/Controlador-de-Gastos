import { BrlPipe } from './brl.pipe';

describe('BrlPipe', () => {
  const pipe = new BrlPipe();

  it('formata em reais', () => {
    // O separador do Intl e um espaco nao-quebravel; normalizamos para comparar.
    expect(pipe.transform(1234.5).replace(/\u00a0/g, ' ')).toBe('R$ 1.234,50');
  });

  it('trata nulo como zero', () => {
    expect(pipe.transform(null).replace(/\u00a0/g, ' ')).toBe('R$ 0,00');
  });
});
