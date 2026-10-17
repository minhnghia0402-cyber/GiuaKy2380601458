import React, { useEffect } from 'react';

// Bản web: hiển thị HTML bài đọc trực tiếp (chữ, ảnh base64, chỗ trống Part 6)
const CSS = `
.passage-html { font-size: 15px; line-height: 1.6; color: #1A1D23; font-family: -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; word-wrap: break-word; }
.passage-html p { margin: 0 0 8px 0; }
.passage-html .passage-image { text-align: center; margin: 8px 0; }
.passage-html img { max-width: 100%; height: auto; border-radius: 6px; border: 1px solid #E3E7EE; }
.passage-html .blank { color: #D93025; font-weight: 700; white-space: nowrap; background: #FCE8E6; border-radius: 4px; padding: 0 4px; }
.passage-html table { border-collapse: collapse; width: 100%; }
.passage-html td { border: 1px solid #ccc; padding: 4px 6px; }
`;

function ensureStyle() {
  if (typeof document === 'undefined' || document.getElementById('passage-html-style')) return;
  const el = document.createElement('style');
  el.id = 'passage-html-style';
  el.innerHTML = CSS;
  document.head.appendChild(el);
}

function PassageHtml({ html }) {
  useEffect(ensureStyle, []);
  return <div className="passage-html" dangerouslySetInnerHTML={{ __html: html }} />;
}

export default React.memo(PassageHtml);
