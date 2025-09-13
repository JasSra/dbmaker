import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LoggerService {
  private get debugEnabled() {
    return !!environment?.features?.enableDebugMode;
  }

  debug(...args: any[]) {
    if (this.debugEnabled) {
      // eslint-disable-next-line no-console
      console.debug(...args);
    }
  }

  info(...args: any[]) {
    if (this.debugEnabled) {
      // eslint-disable-next-line no-console
      console.info(...args);
    }
  }

  warn(...args: any[]) {
    // eslint-disable-next-line no-console
    console.warn(...args);
  }

  error(...args: any[]) {
    // eslint-disable-next-line no-console
    console.error(...args);
  }
}
