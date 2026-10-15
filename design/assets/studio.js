// 原型模拟编辑与更新状态，真实文件、下载和安装由桌面应用执行。
const $ = id => document.getElementById(id);
const media = matchMedia('(prefers-color-scheme:dark)');
const names = ['DotNet','Aurora','Ember','Default','Aquatic','Desert','Dusk','NightSky'];
let culture = localStorage.getItem('culture') || 'zh-CN';
let palette = localStorage.getItem('palette') || 'DotNet';
let mode = localStorage.getItem('mode') || 'system';
let state = 'idle', timer, zoom = 100, markdown = false;
const t = key => (window.translations[culture] || window.translations['en-US'])[key] || key;
function applyTheme() {
  const dark = mode === 'dark' || mode === 'system' && media.matches;
  const tokens = (window.palettes[palette] || window.palettes.DotNet)[dark ? 'dark' : 'light'];
  for (const [key,value] of Object.entries(tokens)) document.documentElement.style.setProperty('--'+key,value);
  Object.assign(document.documentElement.dataset,{theme:dark?'dark':'light',palette,mode});
  document.documentElement.style.colorScheme = dark?'dark':'light';
  $('mode').value = mode;
  document.querySelectorAll('.palette').forEach(button => button.classList.toggle('on',button.dataset.palette === palette));
  localStorage.setItem('palette',palette);localStorage.setItem('mode',mode);
}
function translate() {
  document.documentElement.lang = culture;
  document.querySelectorAll('[data-key]').forEach(el => el.textContent = t(el.dataset.key));
  document.querySelectorAll('[data-title]').forEach(el => el.title = t(el.dataset.title));
  $('language').value = culture;
  $('editor-label').textContent = markdown?'Markdown':t('OutlineTab');
  localStorage.setItem('culture',culture);renderUpdate();
}
for (const key of names) {
  const button = document.createElement('button');button.className='palette';button.dataset.palette=key;
  const swatch = document.createElement('i');swatch.style.setProperty('--swatch',window.palettes[key].light.accent);
  const label = document.createElement('span');label.dataset.key='Palette'+key;button.append(swatch,label);
  button.onclick=()=>{palette=key;applyTheme();};$('palettes').append(button);
}
$('mode').onchange=e=>{mode=e.target.value;applyTheme();};media.onchange=applyTheme;
$('language').onchange=e=>{culture=e.target.value;translate();};
function setEditor() {
  $('outline').hidden=markdown;$('files').hidden=true;$('markdown').hidden=!markdown;
  $('files-tab').classList.remove('on');$('outline-tab').classList.add('on');translate();
}
$('files-tab').onclick=()=>{$('files').hidden=false;$('outline').hidden=true;$('markdown').hidden=true;$('files-tab').classList.add('on');$('outline-tab').classList.remove('on');};
$('outline-tab').onclick=setEditor;
$('editor-mode').onclick=()=>{markdown=!markdown;setEditor();};
$('file-open').onclick=()=>$('files-tab').click();
$('edit-open').onclick=setEditor;
$('add').onclick=()=>{const li=document.createElement('li');li.textContent=t('TopicPlaceholder');$('outline').querySelector('ul').append(li);};
$('undo').onclick=()=>{const list=$('outline').querySelector('ul');if(list.children.length>3)list.lastElementChild.remove();};
$('markdown').oninput=e=>{
  const title=e.target.value.split('\n').find(line=>/^# /.test(line))?.slice(2)||t('Untitled');
  $('outline').querySelector('h3').textContent=title;
  document.querySelectorAll('svg text:first-of-type').forEach(text=>text.textContent=title);
};
$('view-open').onclick=()=>{const hidden=document.querySelector('.side').hidden=!document.querySelector('.side').hidden;document.querySelector('.body').style.gridTemplateColumns=hidden?'0 0 minmax(420px,1fr)':'';};
function setZoom(value){zoom=Math.max(50,Math.min(180,value));$('zoom').textContent=zoom+'%';document.querySelector('.canvas svg').style.transform=`scale(${zoom/100})`;}
$('zoom-out').onclick=()=>setZoom(zoom-10);$('zoom-in').onclick=()=>setZoom(zoom+10);$('center').onclick=()=>setZoom(100);
$('minimap-toggle').onclick=()=>{$('minimap').hidden=!$('minimap').hidden;};
$('splitter').onpointerdown=e=>{
  const start=e.clientX,width=document.querySelector('.side').getBoundingClientRect().width;
  e.target.setPointerCapture(e.pointerId);
  e.target.onpointermove=move=>{document.querySelector('.body').style.gridTemplateColumns=Math.max(280,Math.min(560,width+move.clientX-start))+'px 14px minmax(420px,1fr)';};
  e.target.onpointerup=()=>{e.target.onpointermove=null;};
};
function renderUpdate() {
  const action={idle:'UpdateCheck',current:'UpdateCheck',checking:'UpdateCheck',available:'UpdateDownload',downloading:'UpdateDownload',downloaded:'UpdateInstall',failed:'AboutRecheck',canceled:'AboutRecheck',manual:'UpdateReleasePage',installed:'UpdateReleasePage'};
  const status={idle:'AboutUpdateDescription',current:'AboutCurrent',checking:'AboutChecking',available:'AboutAvailable',downloading:'AboutDownloading',downloaded:'AboutDownloaded',failed:'AboutFailed',canceled:'AboutCanceled',manual:'UpdateReleasePage',installed:'UpdateReleasePage'};
  $('update-action').textContent=t(action[state]);$('update-status').textContent=t(status[state]).replace(/\{0\}/g,state==='available'?'99.0.0':'offline');
  const busy=['checking','downloading'].includes(state);$('update-action').disabled=busy;$('update-cancel').hidden=!busy;$('progress').hidden=state!=='downloading';
  $('about').dataset.state=state;
}
$('about-open').onclick=()=>$('about').showModal();
$('about-close').onclick=()=>{clearTimeout(timer);if(['checking','downloading'].includes(state))state='canceled';$('about').close();};
$('update-action').onclick=()=>{
  if(['idle','current','failed','canceled'].includes(state)){
    state='checking';renderUpdate();timer=setTimeout(()=>{const scenario=new URLSearchParams(location.search).get('update');state=scenario==='latest'?'current':scenario==='offline'?'failed':scenario==='noasset'?'manual':'available';renderUpdate();},220);
  } else if(state==='available'){
    state='downloading';$('progress').value=0;renderUpdate();timer=setTimeout(()=>{$('progress').value=100;state='downloaded';renderUpdate();},300);
  } else if(state==='downloaded'){state='installed';renderUpdate();}
};
$('update-cancel').onclick=()=>{clearTimeout(timer);state='canceled';renderUpdate();};
translate();applyTheme();
