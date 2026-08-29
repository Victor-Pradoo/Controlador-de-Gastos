import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../../shared/ui/toast.service';

/**
 * A API devolve ProblemDetails com `title` = codigo do erro e `detail` = mensagem
 * ja escrita para o usuario. Aqui so exibimos - a feature nao precisa saber disso.
 */
export const httpErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const toast = inject(ToastService);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      const detail = error.error?.detail as string | undefined;

      toast.show(detail ?? 'Nao foi possivel completar a operacao.', 'error');

      return throwError(() => error);
    }),
  );
};
