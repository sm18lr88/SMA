'use strict';

onPluginDisconnected();

if (!nativePort) {
    nativeConnect();
}
