import { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api, getPage } from '../api';

function errorText(error) { return error?.response?.data?.detail || error?.response?.data?.title || error?.response?.data?.error || error?.message || 'The request failed.'; }
function dateValue(value, type) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return '';
  if (type === 'datetime-local') return new Date(date.valueOf() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
  return date.toISOString().slice(0, 10);
}
function display(value) {
  if (value == null || value === '') return '—';
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  if (typeof value === 'number' && value > 999) return new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(value);
  if (typeof value === 'string' && value.includes('T') && !Number.isNaN(Date.parse(value))) return new Date(value).toLocaleDateString();
  return String(value).replace(/([A-Z])/g, ' $1');
}

function RecordForm({ fields, initial, onSave, onCancel }) {
  const [values, setValues] = useState(() => Object.fromEntries(fields.map(field => [field.name, field.type.includes('date') ? dateValue(initial?.[field.name], field.type) : (initial?.[field.name] ?? field.default ?? '')])));
  const [error, setError] = useState(''); const [saving, setSaving] = useState(false); const [lookups,setLookups]=useState({});
  const lookupKey=fields.map(x=>x.relation).filter(Boolean).sort().join(',');
  useEffect(()=>{const types=[...new Set(lookupKey.split(',').filter(Boolean))];if(!types.length)return;Promise.all(types.map(async type=>{const page=await getPage(`/${type}`,{page:1,pageSize:100});return[type,page.items.map(row=>({value:row.id,label:type==='customers'?`${row.firstName} ${row.lastName}${row.companyName?` · ${row.companyName}`:''}`:row.title||row.fullName||`#${row.id}`}))]})).then(entries=>setLookups(Object.fromEntries(entries))).catch(e=>setError(errorText(e)))},[lookupKey]);
  async function submit(event) {
    event.preventDefault(); setSaving(true); setError('');
    const body = {};
    for (const field of fields) {
      let value = values[field.name];
      if (value === '') value = null;
      else if (field.type === 'number' || field.type === 'relation') value = Number(value);
      else if (field.type === 'datetime-local' && value) value = new Date(value).toISOString();
      else if (field.type === 'date' && value) value = new Date(`${value}T00:00:00.000Z`).toISOString();
      else if (field.type === 'boolean') value = Boolean(value);
      body[field.name] = value;
    }
    try { await onSave(body); } catch (e) { setError(errorText(e)); } finally { setSaving(false); }
  }
  return <form onSubmit={submit} className="record-form"><div className="row g-3">{fields.map(field => <div className={field.wide ? 'col-12' : 'col-md-6'} key={field.name}>{field.type==='boolean'?<div className="form-check mt-4"><input id={field.name} className="form-check-input" type="checkbox" checked={Boolean(values[field.name])} onChange={e=>setValues(v=>({...v,[field.name]:e.target.checked}))}/><label htmlFor={field.name} className="form-check-label">{field.label}</label></div>:<><label htmlFor={field.name} className="form-label">{field.label}</label>{field.options||field.relation ? <select id={field.name} className="form-select" required={field.required} value={values[field.name] ?? ''} onChange={e => setValues(v => ({ ...v, [field.name]: e.target.value }))}><option value="">Select…</option>{(field.options||lookups[field.relation]||[]).map(option => <option key={option.value} value={option.value}>{option.label}</option>)}</select> : <input id={field.name} className="form-control" type={field.type} required={field.required} maxLength={field.maxLength} min={field.min} step={field.step} value={values[field.name] ?? ''} placeholder={field.placeholder} onChange={e => setValues(v => ({ ...v, [field.name]: e.target.value }))}/>}</>}</div>)}</div>{error && <div className="alert alert-danger mt-3 mb-0" role="alert">{error}</div>}<div className="d-flex gap-2 mt-4"><button className="btn btn-primary" disabled={saving}>{saving ? 'Saving…' : 'Save'}</button><button type="button" className="btn btn-light" onClick={onCancel}>Cancel</button></div></form>
}

export default function ModulePage({ title, path, columns, fields, search = false, rowAction, readOnly = false }) {
  const [searchParams] = useSearchParams();
  const relatedFilters = Object.fromEntries(['customerId','leadId','opportunityId'].filter(key => searchParams.has(key)).map(key => [key, searchParams.get(key)]));
  const filterKey = JSON.stringify(relatedFilters);
  const [page, setPage] = useState(1); const [query, setQuery] = useState(''); const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true); const [error, setError] = useState(''); const [editing, setEditing] = useState(null); const [creating, setCreating] = useState(false);
  const load = useCallback(async () => { setLoading(true); setError(''); try { const result = await getPage(path, { page, pageSize: 10, ...JSON.parse(filterKey), ...(search ? { search: query } : {}) }); setData(result); } catch (e) { setError(errorText(e)); } finally { setLoading(false); } }, [path, page, query, search, filterKey]);
  useEffect(() => { load(); }, [load]);
  async function save(body) { if (editing) await api.put(`${path}/${editing.id}`, body); else await api.post(path, body); setEditing(null); setCreating(false); await load(); }
  async function remove(row) { if (!window.confirm(`Delete “${row.title || row.fullName || row.description || 'this record'}”?`)) return; try { await api.delete(`${path}/${row.id}`); await load(); } catch (e) { setError(errorText(e)); } }
  async function action(row) { try { await rowAction.run(row); await load(); } catch (e) { setError(errorText(e)); } }
  return <><div className="page-heading"><div><small className="eyebrow">WORKSPACE</small><h1>{title}</h1><p>Manage and review your {title.toLowerCase()}.</p></div><button className="btn btn-primary" onClick={() => { setCreating(true); setEditing(null); }}>+ Add {title.toLowerCase().replace(/s$/, '')}</button></div>
    {error && <div className="alert alert-danger" role="alert">{error}</div>}
    {(creating || editing) && <section className="card mb-3"><div className="card-body"><h2 className="h5 mb-3">{editing ? `Edit ${title.toLowerCase().replace(/s$/, '')}` : `New ${title.toLowerCase().replace(/s$/, '')}`}</h2><RecordForm fields={fields} initial={editing} onSave={save} onCancel={() => { setEditing(null); setCreating(false); }}/></div></section>}
    <section className="card"><div className="card-body">{search && <div className="d-flex gap-2 mb-3"><input className="form-control search" aria-label={`Search ${title}`} placeholder={`Search ${title.toLowerCase()}…`} value={query} onChange={e => { setPage(1); setQuery(e.target.value); }}/></div>}{loading ? <div className="py-5 text-center text-secondary"><span className="spinner-border spinner-border-sm me-2"/>Loading {title.toLowerCase()}…</div> : data?.items?.length ? <div className="table-responsive"><table className="table align-middle"><thead><tr>{columns.map(column => <th key={column.key}>{column.label}</th>)}<th className="text-end">Actions</th></tr></thead><tbody>{data.items.map(row => <tr key={row.id}>{columns.map(column => <td key={column.key}>{column.render ? column.render(row) : display(row[column.key])}</td>)}<td className="text-end text-nowrap">{!readOnly&&<><button className="btn btn-sm btn-link" onClick={() => { setEditing(row); setCreating(false); }}>Edit</button><button className="btn btn-sm btn-link text-danger" onClick={() => remove(row)}>Delete</button></>}{rowAction && <button className="btn btn-sm btn-link" onClick={() => action(row)}>{rowAction.label}</button>}</td></tr>)}</tbody></table></div> : <div className="text-center text-secondary py-5">No {title.toLowerCase()} found.</div>}<div className="d-flex justify-content-between align-items-center mt-2"><small className="text-secondary">{data ? `${data.totalCount} records · page ${data.pageNumber}` : ''}</small><div className="btn-group"><button className="btn btn-sm btn-outline-secondary" disabled={page <= 1 || loading} onClick={() => setPage(p => p - 1)}>Previous</button><button className="btn btn-sm btn-outline-secondary" disabled={!data || page * data.pageSize >= data.totalCount || loading} onClick={() => setPage(p => p + 1)}>Next</button></div></div></div></section></>;
}
