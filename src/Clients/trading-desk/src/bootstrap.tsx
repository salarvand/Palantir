import React from 'react';
import ReactDOM from 'react-dom/client';
import TradingDeskApp from './TradingDeskApp';

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);
root.render(
  <React.StrictMode>
    <TradingDeskApp />
  </React.StrictMode>
);
