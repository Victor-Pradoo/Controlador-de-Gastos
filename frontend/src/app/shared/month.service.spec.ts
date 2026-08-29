import { TestBed } from '@angular/core/testing';
import { MonthService } from './month.service';

describe('MonthService', () => {
  let service: MonthService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MonthService);
  });

  it('comeca no mes corrente', () => {
    const now = new Date();
    const expected = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;

    expect(service.month()).toBe(expected);
  });

  it('formata o rotulo em portugues', () => {
    service.select('2026-08');

    expect(service.label()).toBe('Ago 2026');
  });

  it('vira o ano ao avancar de dezembro', () => {
    service.select('2026-12');
    service.shift(1);

    expect(service.month()).toBe('2027-01');
  });

  it('vira o ano ao voltar de janeiro', () => {
    service.select('2026-01');
    service.shift(-1);

    expect(service.month()).toBe('2025-12');
  });
});
