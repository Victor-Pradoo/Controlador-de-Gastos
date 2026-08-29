import { HttpInterceptorFn } from '@angular/common/http';

/**
 * TEMPORARIO: enquanto nao ha login, identifica o usuario por header.
 * Quando o modulo de identidade entrar, este interceptor passa a anexar o
 * Bearer token e nada mais no app muda.
 */
export const devUserInterceptor: HttpInterceptorFn = (request, next) => {
  const userId = localStorage.getItem('devUserId');

  return userId
    ? next(request.clone({ setHeaders: { 'X-User-Id': userId } }))
    : next(request);
};
