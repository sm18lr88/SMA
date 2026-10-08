'use strict';

var nativePort = null;

function error(msg) {
	console.error(msg);
}

function warn(msg) {
	console.warn(msg)
}

function log(msg) {
	if (app.isLogEnabled)
		console.info(msg);
}

function verbose(msg) {
	if (app.isVerboseLogEnabled)
		console.log(msg);
}

function objectHas(object, key) {
	return object ? hasOwnProperty.call(object, key) : false;
}

function onPluginConnected() {
	chrome.browserAction.setTitle({ title: app.name + " (connected)" });
	chrome.browserAction.setIcon(robotIcons);
	chrome.browserAction.enable();
}

function onPluginDisconnected() {
	chrome.browserAction.setTitle({ title: app.name + " (disconnected)" });
	chrome.browserAction.setIcon(robotDisabledIcons);
	chrome.browserAction.disable();
}