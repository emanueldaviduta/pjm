import { HttpErrorResponse } from '@angular/common/http';

export type LoadState = 'idle' | 'loading' | 'ready' | 'unauthorized' | 'error';

export function errorState(error: unknown): LoadState {
  return error instanceof HttpErrorResponse && error.status === 401 ? 'unauthorized' : 'error';
}
