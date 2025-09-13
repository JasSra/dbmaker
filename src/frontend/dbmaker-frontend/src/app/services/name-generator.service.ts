import { Injectable } from '@angular/core';

export interface NameGeneratorOptions {
  prefix?: string;
  includeNumbers?: boolean;
  length?: 'short' | 'medium' | 'long';
}

@Injectable({
  providedIn: 'root'
})
export class NameGeneratorService {

  private readonly adjectives = [
    // Tech/Modern adjectives
    'blazing', 'quantum', 'neural', 'cyber', 'digital', 'smart', 'auto', 'hyper',
    'ultra', 'mega', 'super', 'turbo', 'rapid', 'swift', 'lightning', 'atomic',

    // Positive adjectives
    'awesome', 'brilliant', 'stellar', 'epic', 'mighty', 'powerful', 'elegant',
    'graceful', 'noble', 'supreme', 'prime', 'elite', 'ultimate', 'perfect',

    // Color-inspired
    'crimson', 'azure', 'golden', 'silver', 'emerald', 'sapphire', 'amber',
    'violet', 'crystal', 'pearl', 'onyx', 'jade', 'ruby', 'diamond',

    // Nature-inspired
    'mountain', 'ocean', 'forest', 'desert', 'arctic', 'tropical', 'lunar',
    'solar', 'stellar', 'cosmic', 'nebula', 'galaxy', 'aurora', 'thunder'
  ];

  private readonly nouns = [
    // Tech/Database related
    'engine', 'core', 'hub', 'node', 'cluster', 'vault', 'cache', 'store',
    'depot', 'warehouse', 'repository', 'database', 'server', 'service',
    'gateway', 'portal', 'bridge', 'link', 'connector', 'adapter',

    // Abstract concepts
    'forge', 'factory', 'lab', 'studio', 'workshop', 'station', 'center',
    'base', 'platform', 'framework', 'system', 'network', 'grid', 'matrix',

    // Animals (friendly/strong)
    'falcon', 'eagle', 'wolf', 'tiger', 'panther', 'shark', 'dolphin',
    'whale', 'phoenix', 'dragon', 'griffin', 'hawk', 'raven', 'cobra',

    // Objects/Tools
    'hammer', 'anvil', 'blade', 'shield', 'beacon', 'tower', 'fortress',
    'citadel', 'palace', 'temple', 'shrine', 'monument', 'crystal', 'prism'
  ];

  private readonly techPrefixes = [
    'db', 'sql', 'data', 'cache', 'store', 'repo', 'vault', 'hub',
    'node', 'core', 'sys', 'net', 'web', 'api', 'app', 'dev',
    'prod', 'test', 'stage', 'demo', 'beta', 'alpha', 'exp'
  ];

  generateFriendlyName(options: NameGeneratorOptions = {}): string {
    const {
      prefix,
      includeNumbers = true,
      length = 'medium'
    } = options;

    let adjective: string;
    let noun: string;

    // Select words based on length preference
    switch (length) {
      case 'short':
        adjective = this.getRandomItem(this.adjectives.filter(a => a.length <= 6));
        noun = this.getRandomItem(this.nouns.filter(n => n.length <= 6));
        break;
      case 'long':
        adjective = this.getRandomItem(this.adjectives.filter(a => a.length >= 6));
        noun = this.getRandomItem(this.nouns.filter(n => n.length >= 6));
        break;
      default: // medium
        adjective = this.getRandomItem(this.adjectives);
        noun = this.getRandomItem(this.nouns);
    }

    let name = `${adjective}-${noun}`;

    // Add prefix if specified
    if (prefix) {
      name = `${prefix}-${name}`;
    }

    // Add numbers if requested
    if (includeNumbers) {
      const number = Math.floor(Math.random() * 99) + 1;
      name = `${name}-${number.toString().padStart(2, '0')}`;
    }

    return name;
  }

  generateDatabaseName(databaseType: 'postgresql' | 'redis', options: NameGeneratorOptions = {}): string {
    const typePrefix = databaseType === 'postgresql' ? 'pg' : 'redis';

    return this.generateFriendlyName({
      ...options,
      prefix: options.prefix || typePrefix
    });
  }

  generateTechnicalName(category?: string): string {
    const prefix = category || this.getRandomItem(this.techPrefixes);
    const adjective = this.getRandomItem(this.adjectives.filter(a => a.length <= 7));
    const noun = this.getRandomItem(this.nouns.filter(n => n.length <= 8));
    const number = Math.floor(Math.random() * 999) + 1;

    return `${prefix}-${adjective}-${noun}-${number.toString().padStart(3, '0')}`;
  }

  generateMultipleNames(count: number, options: NameGeneratorOptions = {}): string[] {
    const names = new Set<string>();
    let attempts = 0;
    const maxAttempts = count * 10; // Prevent infinite loop

    while (names.size < count && attempts < maxAttempts) {
      names.add(this.generateFriendlyName(options));
      attempts++;
    }

    return Array.from(names);
  }

  validateContainerName(name: string): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];

    // Container name rules (Docker naming conventions)
    if (name.length < 1) {
      errors.push('Name cannot be empty');
    }

    if (name.length > 63) {
      errors.push('Name must be 63 characters or less');
    }

    if (!/^[a-zA-Z0-9][a-zA-Z0-9_.-]*$/.test(name)) {
      errors.push('Name must start with alphanumeric character and contain only letters, numbers, underscores, periods, and hyphens');
    }

    if (name.startsWith('-') || name.endsWith('-')) {
      errors.push('Name cannot start or end with a hyphen');
    }

    if (name.startsWith('.') || name.endsWith('.')) {
      errors.push('Name cannot start or end with a period');
    }

    return {
      isValid: errors.length === 0,
      errors
    };
  }

  private getRandomItem<T>(array: T[]): T {
    return array[Math.floor(Math.random() * array.length)];
  }

  // Utility method to get name suggestions
  getNameSuggestions(databaseType: 'postgresql' | 'redis', count: number = 5): string[] {
    return this.generateMultipleNames(count, {
      prefix: databaseType === 'postgresql' ? 'pg' : 'redis',
      includeNumbers: true,
      length: 'medium'
    });
  }
}
