'use strict';

// Browser action click
chrome.browserAction.onClicked.addListener(() => {
  chrome.tabs.query({
    active: true,
    currentWindow: true
  }, reqImportTabs);
});

// Context menu
{
	const callback = () => {
		// Import current
		chrome.contextMenus.create({
			id: 'import-current',
			title: app.locale.current,
			contexts: ['link'],
			documentUrlPatterns: ['*://*/*']
		});
		
		// Import all (current window)
		chrome.contextMenus.create({
			id: 'import-call',
			title: app.locale.call,
			contexts: ['browser_action']
		});
		
		// Import all (all windows)
		chrome.contextMenus.create({
			id: 'import-all',
			title: app.locale.all,
			contexts: ['browser_action']
		});
	};
	
	chrome.runtime.onInstalled.addListener(callback);
	chrome.runtime.onStartup.addListener(callback);
}

chrome.contextMenus.onClicked.addListener(info => {
  if (info.menuItemId === 'import-current') {
    reqImportTabs(info);
  }
  else if (info.menuItemId === 'import-all') {
    chrome.tabs.query({
      url: ['*://*/*']
    }, reqImportTabs);
  }
  else if (info.menuItemId === 'import-call') {
    chrome.tabs.query({
      url: ['*://*/*'],
      currentWindow: true
    }, reqImportTabs);
  }
});