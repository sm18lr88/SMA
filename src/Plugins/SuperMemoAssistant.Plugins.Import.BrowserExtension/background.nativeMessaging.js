'use strict';

if (!nativePort) {
    nativeConnect();
}

function nativeConnect() {
	log("(Native Messaging) Connecting to " + app.id);
	
    nativePort = chrome.runtime.connectNative(app.id);

    nativePort.onMessage.addListener(onNativeMessage);
    nativePort.onDisconnect.addListener(onNativeDisconnected);
	
	if (nativePort)
		onNativeConnected();
}

function onNativeMessage(msg) {
		if (objectHas(msg, 'type') == false) {
			warn("(Native Messaging) Invalid response: " + JSON.stringify(msg));
			return;
		}
		
		verbose("(Native Messaging) Response: " + JSON.stringify(msg));
        
        switch (msg.type) {
			case msgTypes.Connect:
				respConnect(msg);
				break;
				
			case msgTypes.Disconnected:
				respDisconnect(msg);
				break;
				
			case msgTypes.GetTabs:
				respGetTabs(msg);
				break;
				
			case msgTypes.ImportTabs:
				respResult(msg, "Import tabs");
				break;
				
			case msgTypes.ImportHtml:
				respResult(msg, "Import html");
				break;
		}
}

function onNativeConnected() {
	log("(Native Messaging) Connected. Waiting for plugin");
	
	reqConnect();
}

function onNativeDisconnected() {
	let delay;
	
	if (chrome.runtime.lastError) {
		error("(Native Messaging) Disconnected with error: " + JSON.stringify(chrome.runtime.lastError));
		
		delay = 30000;
	}
		
	else {
		warn("(Native Messaging) Disconnected");
		
		delay = 2000;
	}
	
	nativePort = null;
	onPluginDisconnected();
	
	setTimeout(nativeConnect, delay);
}
