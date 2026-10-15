// 大纲、Markdown 与脑图使用同一棵树；系统窗口与安装流程仅为离线演示。
const $ = id => document.getElementById(id), media = matchMedia('(prefers-color-scheme:dark)');
const names = ['DotNet','Aurora','Ember','Default','Aquatic','Desert','Dusk','NightSky'];
let culture = localStorage.getItem('culture') || 'zh-CN';
let palette = localStorage.getItem('palette') || 'DotNet', mode = localStorage.getItem('mode') || 'system';
let state = 'idle', timer, zoom = 100, markdown = false, nextId = 1, selected, current = 0;
const t = key => (window.translations[culture] || window.translations['en-US'])[key] || key;
const sampleHint = () => ({'zh-CN':'离线原型：窗口及更新为操作演示；编译时间为固定样例。','zh-Hant':'離線原型：視窗及更新為操作示範；編譯時間為固定樣例。','en-US':'Offline prototype: window and update actions are simulated; build time is a fixed sample.','ja-JP':'オフライン試作：ウィンドウと更新はデモです。ビルド日時は固定サンプルです。'}[culture]);
const node = (text,children=[]) => ({id:String(nextId++),text,children});
const documents = [
  {name:'Studio.md',root:node('Studio',[node('Controls',[node('Mind map'),node('Outline')]),node('Documents',[node('Markdown'),node('OPML')]),node('Appearance',[node('Eight palettes')])]),history:[],future:[]},
  {name:'Meeting.md',root:node('Meeting',[node('Agenda'),node('Decisions'),node('Action Items')]),history:[],future:[]}
];
const doc = () => documents[current], clone = value => JSON.parse(JSON.stringify(value));
function walk(root,visit,parent=null,depth=0){visit(root,parent,depth);root.children.forEach(child=>walk(child,visit,root,depth+1));}
function lookup(id){let found;walk(doc().root,(item,parent,depth)=>{if(item.id===id)found={item,parent,depth};});return found;}
function snapshot(){doc().history.push({root:clone(doc().root),selected});doc().future=[];}
function restoreHistory(redo=false){const from=redo?doc().future:doc().history,to=redo?doc().history:doc().future;if(!from.length)return;to.push({root:clone(doc().root),selected});const previous=from.pop();doc().root=previous.root;selected=previous.selected;render();}
function serialize(root,depth=1){return '#'.repeat(Math.min(depth,6))+' '+root.text+'\n'+(root.note?root.note+'\n':'')+root.children.map(child=>serialize(child,depth+1)).join('');}
function parseMarkdown(text){
  const root=node(t('Untitled')),stack=[{node:root,level:0}];let hasTitle=false,inCode=false;
  for(const line of text.split(/\r?\n/)){
    if(/^\s*```/.test(line)){inCode=!inCode;continue;}if(inCode)continue;
    const heading=/^(#{1,6})\s+(.+)$/.exec(line),list=/^(\s*)[-*+]\s+(.+)$/.exec(line);if(!heading&&!list){if(line.trim()){const item=stack.at(-1).node;item.note=(item.note?item.note+'\n':'')+line;}continue;}
    const level=heading?heading[1].length:2+Math.floor(list[1].replace(/\t/g,'  ').length/2),title=(heading?heading[2]:list[2]).trim();
    if(heading&&level===1&&!hasTitle&&root.children.length===0){root.text=title;hasTitle=true;stack[0].level=1;continue;}
    while(stack.length>1&&stack.at(-1).level>=level)stack.pop();const child=node(title);stack.at(-1).node.children.push(child);stack.push({node:child,level});
  }return root;
}
function mutate(action){snapshot();action();if(!lookup(selected))selected=doc().root.id;render();}
function select(id){selected=id;document.querySelectorAll('[data-node]').forEach(el=>el.classList.toggle('selected',el.dataset.node===id));}
function addNode(sibling=false){mutate(()=>{const found=lookup(selected)||{item:doc().root},child=node(t('TopicPlaceholder'));if(sibling&&found.parent){const list=found.parent.children;list.splice(list.indexOf(found.item)+1,0,child);}else found.item.children.push(child);selected=child.id;});}
function removeNode(){const found=lookup(selected);if(!found?.parent)return;mutate(()=>{found.parent.children.splice(found.parent.children.indexOf(found.item),1);selected=found.parent.id;});}
function moveNode(kind){const found=lookup(selected);if(!found?.parent)return;const list=found.parent.children,index=list.indexOf(found.item);
  if(kind==='Promote'){const grand=lookup(found.parent.id).parent;if(!grand)return;mutate(()=>{list.splice(index,1);grand.children.splice(grand.children.indexOf(found.parent)+1,0,found.item);});}
  if(kind==='Demote'&&index>0)mutate(()=>{list.splice(index,1);list[index-1].children.push(found.item);});
  if((kind==='MoveUp'&&index>0)||(kind==='MoveDown'&&index<list.length-1))mutate(()=>{list.splice(index,1);list.splice(index+(kind==='MoveUp'?-1:1),0,found.item);});
}
function renderOutline(){
  $('outline').replaceChildren();
  function row(item,root=false){const wrap=document.createElement(root?'div':'li'),input=document.createElement('input');input.className='topic-input'+(root?' root-input':'');input.value=item.text;input.dataset.node=item.id;input.classList.toggle('selected',item.id===selected);input.setAttribute('aria-label',root?t('CenterTopicPlaceholder'):t('TopicPlaceholder'));input.onfocus=()=>select(item.id);input.onchange=()=>{if(input.value.trim()!==item.text)mutate(()=>item.text=input.value.trim()||t('TopicPlaceholder'));};wrap.append(input);if(item.children.length){const list=document.createElement('ul');item.children.forEach(child=>list.append(row(child)));wrap.append(list);}return wrap;}
  $('outline').append(row(doc().root,true));
}
function svgElement(tag,attrs){const el=document.createElementNS('http://www.w3.org/2000/svg',tag);for(const [key,value]of Object.entries(attrs))el.setAttribute(key,value);return el;}
function renderMap(){
  const svg=document.querySelector('.canvas svg'),root=doc().root,positions=new Map();let leaf=0,maxDepth=0;
  function place(item,depth=0){maxDepth=Math.max(maxDepth,depth);let y;if(item.children.length){const children=item.children.map(child=>place(child,depth+1));y=(children[0]+children.at(-1))/2;}else y=72+(leaf++)*62;positions.set(item.id,{x:55+depth*210,y,depth});return y;}
  place(root);const height=Math.max(520,leaf*62+80),width=Math.max(800,(maxDepth+1)*210+50),offset=height/2-positions.get(root.id).y;
  for(const p of positions.values())p.y+=offset;
  const nodeWidth=item=>Math.min(190,Math.max(88,[...item.text].length*8.5+30));svg.replaceChildren();svg.setAttribute('viewBox',`0 0 ${width} ${height}`);
  walk(root,(item,parent)=>{if(!parent)return;const a=positions.get(parent.id),b=positions.get(item.id),right=a.x+nodeWidth(parent);svg.append(svgElement('path',{class:'edge',d:`M${right},${a.y} C${right+65},${a.y} ${b.x-65},${b.y} ${b.x},${b.y}`}));});
  walk(root,item=>{const p=positions.get(item.id),group=svgElement('g',{'data-node':item.id,'data-branch':root.children.indexOf(item),class:(p.depth===0?'root-group ':'')+(item.id===selected?'selected':''),role:'treeitem',tabindex:0,'aria-label':item.text}),rect=svgElement('rect',{class:'node '+(p.depth===0?'root':p.depth===1?'branch':'leaf'),x:p.x,y:p.y-22,width:nodeWidth(item),height:44,rx:7}),text=svgElement('text',{x:p.x+13,y:p.y+5}),title=svgElement('title',{});text.textContent=item.text.length>19?item.text.slice(0,18)+'…':item.text;title.textContent=item.text;group.append(rect,text,title);group.onclick=()=>select(item.id);group.ondblclick=()=>{markdown=false;setEditor();document.querySelector(`#outline [data-node="${item.id}"]`).focus();};svg.append(group);});
  const mini=$('minimap').querySelector('svg');mini.setAttribute('viewBox',svg.getAttribute('viewBox'));mini.innerHTML=svg.innerHTML;
}
function renderFiles(){$('files').replaceChildren();documents.forEach((item,index)=>{const button=document.createElement('button');button.className='fileitem'+(current===index?' on':'');button.dataset.document=index;button.textContent=item.name;button.onclick=()=>{current=index;selected=doc().root.id;render();};$('files').append(button);});}
function render(){renderFiles();renderOutline();renderMap();doc().source=serialize(doc().root);$('markdown').value=doc().source;$('undo').disabled=!doc().history.length;$('redo').disabled=!doc().future.length;let count=0;walk(doc().root,()=>count++);$('node-count').textContent=t('NodeSummary').replace('{0}',count);}
function setEditor(){$('outline').hidden=markdown;$('files').hidden=true;$('markdown').hidden=!markdown;$('files-tab').classList.remove('on');$('outline-tab').classList.add('on');$('editor-label').textContent=markdown?'Markdown':t('OutlineTab');}
function notify(message){$('status').removeAttribute('data-key');$('status').textContent=message;}
function newDocument(template){const root=template?parseMarkdown(t(template+'Markdown')):node(t('Untitled'));documents.push({name:(template?t(template):t('Untitled'))+'.md',root,history:[],future:[]});current=documents.length-1;selected=root.id;render();setEditor();}
function download(text,name,type='text/plain'){const url=URL.createObjectURL(new Blob([text],{type})),link=document.createElement('a');link.href=url;link.download=name;link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
function xml(value){return value.replace(/[<>&"']/g,c=>({'<':'&lt;','>':'&gt;','&':'&amp;','"':'&quot;',"'":'&apos;'}[c]));}
const menuGroups=[
 ['FileMenu','file-open',[['New'],['NewFromTemplate',[['ProductPlanTemplate'],['MeetingNotesTemplate'],['StudyNotesTemplate']]],['NewWindow'],['Open'],['Import'],['OpenFolder'],['RecentFiles',[['Studio.md'],['Meeting.md']]],['Save'],['SaveAs'],['Export',[['ExportMarkdown'],['ExportOpml'],['ExportXMind'],['ExportDrawIo']]],['OpenFileLocation'],['Close']]],
 ['EditMenu','edit-open',[['Undo'],['Redo'],['AddSibling'],['AddChild'],['Promote'],['Demote'],['MoveUp'],['MoveDown'],['CopyAsMarkdown'],['DeleteNode']]],
 ['ViewMenu','view-open',[['HideWorkspacePane'],['CenterRoot'],['ZoomOut'],['ZoomIn'],['ResetZoom']]],
 ['HelpMenu','help-open',[['OpenUserManual'],['NewUserGuide'],['Feedback'],['SubmitFeature'],['SubmitPr'],['Github']]],
 ['AboutMenu','about-menu',[['OpenWebsite'],['Changelog'],['Thanks'],['About']]]
];
function closeMenus(){document.querySelectorAll('#menubar details[open]').forEach(el=>el.open=false);}
function menuItems(items){const box=document.createElement('div');box.className='menu';box.setAttribute('role','menu');for(const [key,children]of items){if(children){const sub=document.createElement('details');sub.className='submenu';const summary=document.createElement('summary');summary.textContent=t(key)+'  ›';summary.dataset.command=key;summary.setAttribute('role','menuitem');sub.append(summary,menuItems(children));box.append(sub);}else{const button=document.createElement('button');button.textContent=t(key);button.dataset.command=key;button.setAttribute('role','menuitem');if(key==='About')button.id='about-open';button.onclick=()=>{closeMenus();command(key);};box.append(button);}}return box;}
function renderMenus(){$('menubar').replaceChildren();for(const [key,id,items]of menuGroups){const details=document.createElement('details'),summary=document.createElement('summary');summary.id=id;summary.textContent=t(key);summary.setAttribute('role','menuitem');details.append(summary,menuItems(items));details.ontoggle=()=>{if(details.open){$('appearance').open=false;document.querySelectorAll('#menubar>details').forEach(other=>{if(other!==details)other.open=false;});}};$('menubar').append(details);}}
function command(key){
 if(key==='Undo')return restoreHistory();if(key==='Redo')return restoreHistory(true);if(key==='AddChild')return addNode();if(key==='AddSibling')return addNode(true);if(key==='DeleteNode')return removeNode();
 if(['Promote','Demote','MoveUp','MoveDown'].includes(key))return moveNode(key);
 if(key==='New')return newDocument();if(key.endsWith('Template'))return newDocument(key);
 if(['Studio.md','Meeting.md'].includes(key)){const index=documents.findIndex(item=>item.name===key);if(index>=0){current=index;selected=doc().root.id;render();}return;}
 if(['Open','Import'].includes(key))return $('open-input').click();
 if(['Save','SaveAs'].includes(key))return download(JSON.stringify({name:doc().name,root:doc().root},null,2),doc().name.replace(/\.md$/i,'.json'),'application/json');
 if(key==='ExportMarkdown')return download(serialize(doc().root),doc().name,'text/markdown');
 if(key==='ExportOpml'){const outline=item=>`<outline text="${xml(item.text)}">${item.children.map(outline).join('')}</outline>`;return download(`<?xml version="1.0" encoding="UTF-8"?><opml version="2.0"><head><title>${xml(doc().root.text)}</title></head><body>${outline(doc().root)}</body></opml>`,doc().name.replace(/\.md$/i,'.opml'),'text/xml');}
 if(key==='CopyAsMarkdown'){const text=serialize((lookup(selected)||{item:doc().root}).item);navigator.clipboard?.writeText(text).then(()=>notify(t(key))).catch(()=>notify(text));return;}
 if(key==='Close'){if(documents.length>1){documents.splice(current,1);current=Math.max(0,current-1);selected=doc().root.id;render();}else newDocument();return;}
 if(key==='HideWorkspacePane'){const hidden=document.querySelector('.side').hidden=!document.querySelector('.side').hidden;document.querySelector('.body').style.gridTemplateColumns=hidden?'0 0 minmax(420px,1fr)':'';return;}
 if(['CenterRoot','ResetZoom'].includes(key))return setZoom(100);if(key==='ZoomIn')return setZoom(zoom+10);if(key==='ZoomOut')return setZoom(zoom-10);
 if(key==='About')return $('about').showModal();notify(t(key)+' · '+sampleHint());
}
function applyTheme(){const dark=mode==='dark'||mode==='system'&&media.matches,tokens=(window.palettes[palette]||window.palettes.DotNet)[dark?'dark':'light'];for(const [key,value]of Object.entries(tokens))document.documentElement.style.setProperty('--'+key,value);Object.assign(document.documentElement.dataset,{theme:dark?'dark':'light',palette,mode});document.documentElement.style.colorScheme=dark?'dark':'light';$('mode').value=mode;document.querySelectorAll('.palette').forEach(button=>button.classList.toggle('on',button.dataset.palette===palette));localStorage.setItem('palette',palette);localStorage.setItem('mode',mode);}
function translate(){document.documentElement.lang=culture;document.querySelectorAll('[data-key]').forEach(el=>el.textContent=t(el.dataset.key));document.querySelectorAll('[data-title]').forEach(el=>el.title=t(el.dataset.title));$('language').value=culture;$('editor-label').textContent=markdown?'Markdown':t('OutlineTab');$('prototype-hint').textContent=sampleHint();localStorage.setItem('culture',culture);renderMenus();renderUpdate();render();}
for(const key of names){const button=document.createElement('button');button.className='palette';button.dataset.palette=key;const swatch=document.createElement('i');swatch.style.setProperty('--swatch',window.palettes[key].light.accent);const label=document.createElement('span');label.dataset.key='Palette'+key;button.append(swatch,label);button.onclick=()=>{palette=key;applyTheme();};$('palettes').append(button);}
$('appearance').ontoggle=()=>{if($('appearance').open)closeMenus();};$('mode').onchange=e=>{mode=e.target.value;applyTheme();};media.onchange=applyTheme;$('language').onchange=e=>{culture=e.target.value;translate();};
$('files-tab').onclick=()=>{$('files').hidden=false;$('outline').hidden=true;$('markdown').hidden=true;$('files-tab').classList.add('on');$('outline-tab').classList.remove('on');};$('outline-tab').onclick=setEditor;$('editor-mode').onclick=()=>{markdown=!markdown;setEditor();};$('add').onclick=()=>addNode();$('undo').onclick=()=>restoreHistory();$('redo').onclick=()=>restoreHistory(true);
$('markdown').oninput=e=>{if(e.target.value===doc().source)return;snapshot();doc().source=e.target.value;doc().root=parseMarkdown(e.target.value);selected=doc().root.id;renderOutline();renderMap();$('undo').disabled=false;let count=0;walk(doc().root,()=>count++);$('node-count').textContent=t('NodeSummary').replace('{0}',count);};
$('open-input').onchange=async e=>{const file=e.target.files[0];if(!file)return;try{const text=await file.text();let root;if(file.name.endsWith('.json')){const data=JSON.parse(text);const normalize=item=>{if(!item||typeof item.text!=='string'||!Array.isArray(item.children))throw Error('Invalid tree');return {...node(item.text,item.children.map(normalize)),note:typeof item.note==='string'?item.note:undefined};};root=normalize(data.root);}else root=parseMarkdown(text);documents.push({name:file.name,root,history:[],future:[]});current=documents.length-1;selected=root.id;render();setEditor();notify(t('StatusOpened').replace('{0}',file.name));}catch(error){notify(error.message);}e.target.value='';};
function setZoom(value){zoom=Math.max(50,Math.min(180,value));$('zoom').textContent=zoom+'%';document.querySelector('.canvas svg').style.transform=`scale(${zoom/100})`;}
$('zoom-out').onclick=()=>setZoom(zoom-10);$('zoom-in').onclick=()=>setZoom(zoom+10);$('center').onclick=()=>setZoom(100);$('minimap-toggle').onclick=()=>{$('minimap').hidden=!$('minimap').hidden;};
$('splitter').onpointerdown=e=>{const start=e.clientX,width=document.querySelector('.side').getBoundingClientRect().width;e.target.setPointerCapture(e.pointerId);e.target.onpointermove=move=>{document.querySelector('.body').style.gridTemplateColumns=Math.max(280,Math.min(560,width+move.clientX-start))+'px 14px minmax(420px,1fr)';};e.target.onpointerup=()=>{e.target.onpointermove=null;};};
document.querySelectorAll('[data-window]').forEach(button=>button.onclick=()=>{if(button.dataset.window==='maximize')document.querySelector('.win').classList.toggle('maximized');else notify(sampleHint());});
document.querySelectorAll('[data-dialog-window]').forEach(button=>button.onclick=()=>{if(button.dataset.dialogWindow==='maximize')$('about').classList.toggle('maximized');else notify(sampleHint());});
document.addEventListener('click',e=>{if(!e.target.closest('#menubar'))closeMenus();});
document.addEventListener('keydown',e=>{if(e.key==='Escape'){closeMenus();$('appearance').open=false;}if(e.target.closest('[role="menu"]')){const items=[...e.target.closest('[role="menu"]').querySelectorAll(':scope>button,:scope>.submenu>summary')],i=items.indexOf(document.activeElement);if(['ArrowDown','ArrowUp'].includes(e.key)){e.preventDefault();items[(i+(e.key==='ArrowDown'?1:items.length-1))%items.length]?.focus();}if(e.key==='ArrowRight'&&e.target.matches('summary')){e.preventDefault();e.target.parentElement.open=true;e.target.parentElement.querySelector('button')?.focus();}if(e.key==='ArrowLeft'){const parent=e.target.closest('.submenu');if(parent){e.preventDefault();parent.open=false;parent.querySelector('summary').focus();}}return;}if(e.target.closest('textarea,input,select'))return;if(e.ctrlKey||e.metaKey){if(e.key.toLowerCase()==='z'){e.preventDefault();restoreHistory(e.shiftKey);}return;}if(e.key==='Enter'){e.preventDefault();addNode(true);}if(e.key==='Tab'&&e.target.closest('.canvas')){e.preventDefault();addNode();}if(e.key==='Delete'){e.preventDefault();removeNode();}});
function renderUpdate(){const action={idle:'UpdateCheck',current:'UpdateCheck',checking:'UpdateCheck',available:'UpdateDownload',downloading:'UpdateDownload',downloaded:'UpdateInstall',failed:'AboutRecheck',canceled:'AboutRecheck',manual:'UpdateReleasePage',installed:'UpdateReleasePage'},status={idle:'AboutUpdateDescription',current:'AboutCurrent',checking:'AboutChecking',available:'AboutAvailable',downloading:'AboutDownloading',downloaded:'AboutDownloaded',failed:'AboutFailed',canceled:'AboutCanceled',manual:'UpdateReleasePage',installed:'UpdateReleasePage'};$('update-action').textContent=t(action[state]);$('update-status').textContent=t(status[state]).replace(/\{0\}/g,state==='available'?'99.0.0':'offline');const busy=['checking','downloading'].includes(state);$('update-action').disabled=busy;$('update-cancel').hidden=!busy;$('progress').hidden=state!=='downloading';$('about').dataset.state=state;}
$('about-close').onclick=()=>{clearTimeout(timer);if(['checking','downloading'].includes(state))state='canceled';renderUpdate();$('about').close();};
$('update-action').onclick=()=>{if(['idle','current','failed','canceled'].includes(state)){state='checking';renderUpdate();timer=setTimeout(()=>{const scenario=new URLSearchParams(location.search).get('update');state=scenario==='latest'?'current':scenario==='offline'?'failed':scenario==='noasset'?'manual':'available';renderUpdate();},220);}else if(state==='available'){state='downloading';$('progress').value=0;renderUpdate();timer=setTimeout(()=>{$('progress').value=100;state='downloaded';renderUpdate();},300);}else if(state==='downloaded'){state='installed';renderUpdate();}};
$('update-cancel').onclick=()=>{clearTimeout(timer);state='canceled';renderUpdate();};$('about').addEventListener('cancel',()=>{clearTimeout(timer);if(['checking','downloading'].includes(state))state='canceled';renderUpdate();});
$('about-version').textContent=window.prototypeMetadata.version;$('about-build').textContent=window.prototypeMetadata.build;$('about-copyright').textContent=window.prototypeMetadata.copyright;
selected=doc().root.id;translate();applyTheme();
