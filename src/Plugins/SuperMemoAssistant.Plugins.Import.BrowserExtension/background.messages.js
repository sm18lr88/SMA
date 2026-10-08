'use strict';

chrome.runtime.onMessage.addListener((request, sender) => {
  if (request.cmd === 'import') {
    open([request.url], [sender.tab.id]);
  }
});