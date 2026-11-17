import { describe, expect, it } from 'vitest';
import { format, label } from './utils';
describe('CRM table formatters', () => {
  it('renders missing values as a dash and booleans accessibly', () => { expect(format(null)).toBe('—'); expect(format(false)).toBe('No'); });
  it('humanizes field labels', () => { expect(label('expectedCloseDate')).toBe('Expected Close Date'); });
});
