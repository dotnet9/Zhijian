const standaloneFixture = require('./standalone-fixture.cjs');
const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { chromium } = require('playwright');

// 使用真实 DOM 入口验证主题、编辑切换与更新流程，不请求外部站点。
(async () => {
  const standalone = standaloneFixture(path.resolve(__dirname, '../index.html'));
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage({ viewport:{width:1240,height:820} });
    await standalone.attach(page);
    const errors=[];page.on('pageerror',error=>errors.push(error.message));
    const url=standalone.url;
    await page.goto(url);
    await page.locator('#appearance summary').click();
    const palettes=await page.locator('.palette').evaluateAll(items=>items.map(item=>item.dataset.palette));
    assert.equal(palettes.length,8);
    for(const palette of palettes){
      await page.locator(`.palette[data-palette="${palette}"]`).click();
      for(const mode of ['light','dark']){
        await page.locator('#mode').selectOption(mode);
        assert.equal(await page.locator('html').getAttribute('data-theme'),mode);
        assert.equal(await page.locator('html').getAttribute('data-palette'),palette);
      }
    }
    await page.locator('#mode').selectOption('system');
    for(const scheme of ['light','dark']){await page.emulateMedia({colorScheme:scheme});await page.waitForFunction(value=>document.documentElement.dataset.theme===value,scheme);}
    await page.locator('#appearance summary').click();
    for(const culture of ['zh-CN','en-US','zh-Hant','ja-JP']){
      await page.locator('#language').selectOption(culture);
      assert.deepEqual(await page.locator('[data-key]').evaluateAll(items=>items.filter(el=>!Object.hasOwn(window.translations[document.documentElement.lang],el.dataset.key)).map(el=>el.dataset.key)),[]);
      for(const width of [1240,980]){
        await page.setViewportSize({width,height:640});
        assert.ok(await page.locator('.canvas').evaluate(el=>el.getBoundingClientRect().width>400));
        assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
      }
    }
    await page.locator('#editor-mode').click();assert.equal(await page.locator('#markdown').isVisible(),true);
    await page.locator('#markdown').fill('# Prototype sync\n\n## Topic');
    assert.equal(await page.locator('.canvas text').first().textContent(),'Prototype sync');
    await page.locator('#editor-mode').click();assert.equal(await page.locator('#outline').isVisible(),true);
    await page.locator('#minimap-toggle').click();assert.equal(await page.locator('#minimap').isVisible(),true);
    for(const scenario of ['new','latest','offline','noasset']){
      await page.goto(url+'?update='+scenario);await page.locator('#about-menu').click();await page.locator('#about-open').click();await page.locator('#update-action').click();
      await page.waitForFunction(()=>document.querySelector('#about').dataset.state!=='checking');
      const expected={new:'available',latest:'current',offline:'failed',noasset:'manual'}[scenario];
      assert.equal(await page.locator('#about').getAttribute('data-state'),expected);
      if(scenario==='new'){await page.locator('#update-action').click();await page.waitForFunction(()=>document.querySelector('#about').dataset.state==='downloaded');}
      await page.locator('#about-close').click();
    }
    assert.deepEqual(errors,[]);
    await standalone.verify(page);
    console.log('PASS: sixteen palettes, system changes, four languages, two widths, shared editor and four update flows.');
  } finally {await browser.close(); standalone.dispose();}
})().catch(error=>{console.error(error);process.exitCode=1;});
