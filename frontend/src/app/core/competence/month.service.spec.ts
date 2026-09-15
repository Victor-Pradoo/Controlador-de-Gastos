import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { MonthService } from './month.service';
import { CompetenceSettings } from '../../shared/models/competence';

describe('MonthService', () => {
  let service: MonthService;
  let http: HttpTestingController;

  const settings: CompetenceSettings = {
    closingDay: 25,
    current: '2026-09',
    currentWindow: { start: '2026-08-25', end: '2026-09-24' },
    reopenableCompetence: null,
    reopenableClosedOn: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(MonthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('comeca no mes do calendario ate a regra chegar', () => {
    const now = new Date();
    const expected = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;

    expect(service.month()).toBe(expected);
  });

  it('assume a competencia corrente vinda do backend', () => {
    service.load().subscribe();
    http.expectOne((r) => r.url.endsWith('/ledger/competence')).flush(settings);

    expect(service.month()).toBe('2026-09');
    expect(service.competence()?.closingDay).toBe(25);
    expect(service.isCurrent()).toBeTrue();
  });

  it('cai no mes do calendario quando o backend nao responde', () => {
    const before = service.month();

    service.load().subscribe();
    http.expectOne((r) => r.url.endsWith('/ledger/competence'))
      .flush('erro', { status: 500, statusText: 'Server Error' });

    expect(service.month()).toBe(before);
    expect(service.competence()).toBeNull();
  });

  it('expoe a janela da competencia corrente', () => {
    service.load().subscribe();
    http.expectOne((r) => r.url.endsWith('/ledger/competence')).flush(settings);

    expect(service.currentWindow()).toEqual({ start: '2026-08-25', end: '2026-09-24' });
  });

  it('esconde a janela ao navegar para outra competencia', () => {
    service.load().subscribe();
    http.expectOne((r) => r.url.endsWith('/ledger/competence')).flush(settings);

    service.shift(-1);

    expect(service.month()).toBe('2026-08');
    expect(service.isCurrent()).toBeFalse();
    expect(service.currentWindow()).toBeNull();
  });

  it('oferece reabertura so na competencia aberta por um encerramento', () => {
    service.load().subscribe();
    http.expectOne((r) => r.url.endsWith('/ledger/competence')).flush({
      ...settings,
      reopenableCompetence: '2026-08',
      reopenableClosedOn: '2026-08-18',
    });

    expect(service.reopenable()).toBe('2026-08');

    service.shift(-1);
    expect(service.reopenable()).toBeNull();
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
