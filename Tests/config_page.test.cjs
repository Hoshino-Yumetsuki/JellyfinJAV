const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const vm = require('node:vm');

const pluginId = '1d5fffc2-1028-4553-9660-bd4966899e44';
const html = fs.readFileSync(path.join(__dirname, '../JellyfinJav/config_page.html'), 'utf8');
const script = html.match(/<script[^>]*>([\s\S]*?)<\/script>/)[1];

function loadPage(config) {
    const elements = {};
    for (const id of ['JellyfinJavConfigPage', 'JellyfinJavConfigForm', 'actressNameOrder', 'videoDisplayName', 'enableActresses']) {
        elements[id] = {
            value: '',
            checked: false,
            handlers: {},
            addEventListener(event, handler) { this.handlers[event] = handler; }
        };
    }

    const updates = [];
    const processed = [];
    vm.runInNewContext(script, {
        document: { getElementById: id => elements[id] },
        ApiClient: {
            async getPluginConfiguration(id) {
                assert.equal(id, pluginId);
                return { ...config };
            },
            async updatePluginConfiguration(id, updated) {
                assert.equal(id, pluginId);
                updates.push(updated);
                return 'saved';
            }
        },
        Dashboard: { processPluginConfigurationUpdateResult: result => processed.push(result) }
    });
    return { elements, updates, processed };
}

test('settings load using Jellyfin property names without jQuery', async () => {
    const page = loadPage({ ActressNameOrder: 'LastFirst', VideoDisplayName: 'Title', EnableActresses: true });
    page.elements.JellyfinJavConfigPage.handlers.pageshow();
    await new Promise(setImmediate);

    assert.equal(page.elements.actressNameOrder.value, 'LastFirst');
    assert.equal(page.elements.videoDisplayName.value, 'Title');
    assert.equal(page.elements.enableActresses.checked, true);
    assert.equal(page.updates.length, 0);
});

test('saving settings prevents form navigation and preserves other configuration', async () => {
    const page = loadPage({ ActressNameOrder: 'LastFirst', VideoDisplayName: 'Title', EnableActresses: true, OtherSetting: 42 });
    page.elements.actressNameOrder.value = 'FirstLast';
    page.elements.videoDisplayName.value = 'CodeTitle';
    page.elements.enableActresses.checked = false;

    let prevented = false;
    page.elements.JellyfinJavConfigForm.handlers.submit({ preventDefault() { prevented = true; } });
    assert.equal(prevented, true);
    await new Promise(setImmediate);

    assert.deepEqual(page.updates, [{
        ActressNameOrder: 'FirstLast',
        VideoDisplayName: 'CodeTitle',
        EnableActresses: false,
        OtherSetting: 42
    }]);
    assert.deepEqual(page.processed, ['saved']);
});
