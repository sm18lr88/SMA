'use strict';

function reqConnect() {
	var msg = createMsg(msgTypes.Connect, { UserAgent: navigator.userAgent });
	
	log("(Native Messaging) Requesting 'Connect': " + JSON.stringify(msg));
	nativePort.postMessage(msg);
}

function reqImportTabs(tabs) {
	var msg = createMsg(msgTypes.ImportTabs, { Tabs: tabs });
	
	log("(Native Messaging) Requesting 'ImportTabs': " + JSON.stringify(msg));
	nativePort.postMessage(msg);
}

function reqImportHtml(tab, html) {
	var msg = createMsg(msgTypes.ImportHtml, { Tab: tab, Html: html });
	
	log("(Native Messaging) Requesting 'ImportHtml': " + JSON.stringify(msg));
	nativePort.postMessage(msg);
}

function respConnect(msg) {
	if (isMessageValid(msg, "Connect", "version") == false)
		return;
	
	log("(Native Messaging) Plugin version '" + msg.version + "' connected");
	
	onPluginConnected();
}

function respDisconnect(msg) {
	log("(Native Messaging) Plugin disconnected");
	
	onPluginDisconnected();
}

function respGetTabs(msg) {
	log("(Native Messaging) Received 'GetTabs', querying tabs '*://*/*'");
	
    chrome.tabs.query({ url: ['*://*/*'] }, tabs => {
		var msg = createMsg(msgTypes.GetTabs, { Tabs: tabs });
		
		log("(Native Messaging) Replying 'GetTabs': " + JSON.stringify(msg));
		nativePort.postMessage(msg);
	});
}

function respResult(msg, op) {
	if (isMessageValid(msg, op, "success") == false)
		return;
	
	if (msg.success == false)
		warn("(Native Messaging) Operation failed: " + op);
	
	else
		log("(Native Messaging) Operation success: " + op);
}

function createMsg(msgType, data) {
	return { Type: msgType, Data: data };
}

function isMessageValid(msg, op) {
	var propNames = Array.prototype.slice.call(arguments, 2);
	var missingPropNames = [];
	
	propNames.forEach(pn => {
		if (objectHas(msg, pn) == false)
			missingPropNames.push("'" + pn + "'");
	});
	
	if (missingPropNames.length > 0) {
		warn("(Native Messaging) Invalid response: " + missingPropNames.join(", ") + " missing from 'msg' for operation '" + op + "'");
		return false;
	}
	
	return true;
}