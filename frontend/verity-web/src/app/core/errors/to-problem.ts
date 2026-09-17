import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../api/models';

/**
 * Normalises any HttpErrorResponse into a ProblemDetails shape. The API
 * always returns application/problem+json on error, but network failures
 * (no connection, CORS, etc.) never reach the server, so those fall back to
 * a synthetic "Network error" problem instead of a crash.
 */
export function toProblem(error: HttpErrorResponse): ProblemDetails {
  const body = error.error;

  if (body && typeof body === 'object' && 'title' in body) {
    return body as ProblemDetails;
  }

  if (error.status === 0) {
    return {
      title: 'Network error',
      status: 0,
      detail: 'Could not reach the server. Check your connection and try again.',
    };
  }

  return {
    title: error.statusText || 'Something went wrong',
    status: error.status,
  };
}
