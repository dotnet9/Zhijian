const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path');
const {chromium}=require('playwright');
const fixture=require('./standalone-fixture.cjs');
(async()=>{
 const standalone=fixture(path.resolve(__dirname,'../index.html')),browser=await chromium.launch({headless:true});
 const output=process.argv[2];if(output)fs.mkdirSync(output,{recursive:true});
 try{
  const page=await browser.newPage({viewport:{width:1280,height:840}}),errors=[];await standalone.attach(page);page.on('pageerror',e=>errors.push(e.message));await page.goto(standalone.url);
  await page.addStyleTag({content:'*,*::before,*::after{animation:none!important;transition:none!important}'});
  const graph=()=>page.locator('.canvas svg [role=treeitem]');
  assert.equal(await page.locator('#mode-toggle').count(),1);assert.equal(await page.locator('#palettes button').count(),8);
  assert.equal(await page.locator('.titlebar [data-window]').count(),3);assert.equal(await page.locator('.titlebar').evaluate(el=>el.getBoundingClientRect().height),40);
  assert.ok(await page.locator('.win').evaluate(el=>parseFloat(getComputedStyle(el).borderTopWidth)>=1));
  if(output)await page.screenshot({path:path.join(output,'studio-initial.png')});
  const expected=['New','ProductPlanTemplate','MeetingNotesTemplate','StudyNotesTemplate','NewWindow','Open','Import','OpenFolder','RecentFiles','Save','SaveAs','ExportMarkdown','ExportOpml','ExportXMind','ExportDrawIo','OpenFileLocation','Close','Undo','Redo','AddSibling','AddChild','Promote','Demote','MoveUp','MoveDown','CopyAsMarkdown','DeleteNode','HideWorkspacePane','CenterRoot','ZoomOut','ZoomIn','ResetZoom','OpenUserManual','NewUserGuide','Feedback','SubmitFeature','SubmitPr','Github','OpenWebsite','Changelog','Thanks','About'];
  for(const key of expected)assert.equal(await page.locator(`[data-command="${key}"]`).count(),1,key);
  for(const language of ['zh-CN','zh-Hant','en-US','ja-JP']){
   await page.locator('#language-menu summary').click();await page.locator('[data-language="'+language+'"]').click();
   assert.equal(await page.locator('#file-open').textContent(),await page.evaluate(()=>translations[document.documentElement.lang].FileMenu));
   await page.locator('#about-menu').click();await page.locator('#about-open').click();
   assert.equal(await page.locator('#about-build').textContent(),'2026-10-15 00:00:00');
   assert.equal(await page.locator('#about-version').textContent(),await page.evaluate(()=>prototypeMetadata.version));
   assert.ok((await page.locator('#about-copyright').textContent()).includes('codewf.com'));
   assert.equal(await page.locator('#about .caption button').count(),3);
   assert.equal(await page.locator('#about [data-key=AboutWindowTitle]').textContent(),await page.evaluate(()=>translations[document.documentElement.lang].AboutWindowTitle));
   assert.ok(await page.locator('#about').evaluate(el=>parseFloat(getComputedStyle(el).borderTopWidth)>=1));
   if(output)await page.screenshot({path:path.join(output,'about-'+language+'.png')});await page.locator('#about-close').click();
  }
  await page.locator('#language-menu summary').click();await page.locator('[data-language="en-US"]').click();
  await page.locator('#file-open').click();await page.locator('[data-command=NewFromTemplate]').click();
  await page.locator('[data-command=MeetingNotesTemplate]').click();assert.equal(await page.locator('.canvas text').first().textContent(),'Meeting Notes');assert.ok(await graph().count()>10);
  await page.locator('#files-tab').click();await page.locator('.fileitem').filter({hasText:'Studio.md'}).click();await page.locator('#outline-tab').click();
  const initial=await graph().count();await page.locator('#add').click();assert.equal(await graph().count(),initial+1);
  const topic=page.locator('#outline input').last();await topic.fill('Shared child 中文 😀');await topic.press('Tab');
  assert.ok((await page.locator('.canvas svg').textContent()).includes('Shared child 中文 😀'));
  await page.locator('#editor-mode').click();assert.ok((await page.locator('#markdown').inputValue()).includes('## Shared child 中文 😀'));
  await page.locator('#markdown').fill('# Synchronized\nRoot note retained\n## First\n### Nested\n## Second\n- Listed\n  - Deep list');
  assert.equal(await graph().count(),6);assert.equal(await page.locator('.canvas text').first().textContent(),'Synchronized');
  await page.locator('#editor-mode').click();assert.equal(await page.locator('#outline input').count(),6);
  await page.locator('#undo').click();assert.ok((await page.locator('.canvas svg').textContent()).includes('Shared child 中文 😀'));
  await page.locator('#redo').click();assert.equal(await page.locator('.canvas text').first().textContent(),'Synchronized');
  await page.locator('#editor-mode').click();assert.ok((await page.locator('#markdown').inputValue()).includes('Root note retained'));await page.locator('#editor-mode').click();
  await page.locator('#files-tab').click();await page.locator('.fileitem').filter({hasText:'Meeting.md'}).click();assert.equal(await page.locator('.canvas text').first().textContent(),'Meeting');
  await page.locator('.fileitem').filter({hasText:'Studio.md'}).click();assert.equal(await page.locator('.canvas text').first().textContent(),'Synchronized');await page.locator('#outline-tab').click();
  const child=page.locator('#outline input').nth(1);await child.focus();const before=await graph().count();
  await page.locator('#edit-open').click();await page.locator('[data-command=AddSibling]').click();assert.equal(await graph().count(),before+1);
  await page.locator('#edit-open').click();await page.locator('[data-command=Demote]').click();
  await page.locator('#editor-mode').click();assert.ok((await page.locator('#markdown').inputValue()).includes('### Topic'));await page.locator('#editor-mode').click();
  await page.locator('#edit-open').click();await page.locator('[data-command=Promote]').click();
  await page.locator('#edit-open').click();await page.locator('[data-command=DeleteNode]').click();assert.equal(await graph().count(),before);
  await page.locator('#minimap-toggle').click();assert.equal(await page.locator('#minimap [role=treeitem]').count(),before);
  await page.locator('#open-input').setInputFiles({name:'Imported.md',mimeType:'text/markdown',buffer:Buffer.from('# Imported\n## Child\n### Grandchild')});
  await page.waitForFunction(()=>document.querySelector('.canvas text').textContent==='Imported');assert.equal(await graph().count(),3);
  await page.locator('#file-open').click();await page.locator('[data-command=Export]').click();
  const exportEvent=page.waitForEvent('download');await page.locator('[data-command=ExportOpml]').click();const exported=await exportEvent;assert.ok(exported.suggestedFilename().endsWith('.opml'));
  if(output){await exported.saveAs(path.join(output,'exported.opml'));assert.ok(fs.readFileSync(path.join(output,'exported.opml'),'utf8').includes('Grandchild'));}
  const splitter=await page.locator('#splitter').boundingBox();await page.mouse.move(splitter.x+7,splitter.y+40);await page.mouse.down();await page.mouse.move(splitter.x+77,splitter.y+40);await page.mouse.up();assert.equal(Math.round(await page.locator('.side').evaluate(el=>el.getBoundingClientRect().width)),430);
  await page.locator('#minimap-toggle').click();
  for(const width of [1280,980]){await page.setViewportSize({width,height:840});assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);assert.ok(await page.evaluate(()=>document.querySelector('#menubar>details:nth-child(5)').getBoundingClientRect().right<=document.querySelector('.studio-title-controls').getBoundingClientRect().left));if(output)await page.screenshot({path:path.join(output,'desktop-'+width+'.png')});}
  await page.locator('#about-menu').click();await page.locator('#about-open').click();await page.locator('#update-action').click();await page.locator('#update-cancel').click();assert.equal(await page.locator('#about').getAttribute('data-state'),'canceled');await page.locator('#about-close').click();
  assert.deepEqual(errors,[]);await standalone.verify(page);
  console.log('PASS: desktop frames/time, 42 native menu entries, four literal languages, three-way tree edits, history, file drafts/import, OPML export, pointer splitter and cancellation');
 }finally{await browser.close();standalone.dispose();}
})().catch(e=>{console.error(e);process.exitCode=1;});
