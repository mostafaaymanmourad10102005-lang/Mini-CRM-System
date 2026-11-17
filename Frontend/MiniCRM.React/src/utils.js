export function format(value) { if (value == null || value === '') return '—'; if (typeof value === 'boolean') return value ? 'Yes' : 'No'; return String(value).replace(/([A-Z])/g, ' $1'); }
export function label(value) { return value.replace(/([A-Z])/g, ' $1').replace(/^./, x => x.toUpperCase()); }
