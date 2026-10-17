import React from 'react';
import { View } from 'react-native';
import { WebView } from 'react-native-webview';

// Bản điện thoại (Android/iOS): hiển thị HTML bài đọc bằng WebView
function PassageHtml({ html }) {
  const page = `<!doctype html><html><head><meta name="viewport" content="width=device-width, initial-scale=1">
  <style>
    body { font-family: -apple-system, Roboto, sans-serif; font-size: 15px; line-height: 1.6; color: #1A1D23; margin: 0; padding: 4px; }
    p { margin: 0 0 8px 0; } img { max-width: 100%; height: auto; }
    .passage-image { text-align: center; }
    .blank { color: #D93025; font-weight: 700; background: #FCE8E6; border-radius: 4px; padding: 0 4px; }
  </style></head><body>${html}</body></html>`;
  return (
    <View style={{ height: 380 }}>
      <WebView originWhitelist={['*']} source={{ html: page }} />
    </View>
  );
}

export default React.memo(PassageHtml);
