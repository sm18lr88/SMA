'use strict';

const msgTypes = {
	Connect: 1,
	Disconnect: 2,
	
	ImportTabs: 100,
	ImportHtml: 101,
	
	GetTabs: 200
};

const robotIcons = {
	path: {
      "16": "data/icons/robot-16.png",
      "24": "data/icons/robot-24.png",
      "32": "data/icons/robot-32.png",
      "48": "data/icons/robot-48.png",
      "64": "data/icons/robot-64.png",
      "128": "data/icons/robot-128.png"
    }
};

const robotDisabledIcons = {
	path: {
      "16": "data/icons/robot-hollow-16.png",
      "24": "data/icons/robot-hollow-24.png",
      "32": "data/icons/robot-hollow-32.png",
      "64": "data/icons/robot-hollow-64.png",
      "128": "data/icons/robot-hollow-128.png"
    }
};