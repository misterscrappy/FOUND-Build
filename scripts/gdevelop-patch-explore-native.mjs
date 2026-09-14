import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const projectPath = process.argv[2] || 'game.json';
const game = JSON.parse(fs.readFileSync(projectPath, 'utf8'));
const statePaths = JSON.parse(fs.readFileSync('gdevelop-source/us-state-paths.json', 'utf8'));
if (Object.keys(statePaths).length !== 50) throw new Error(`Expected 50 states, got ${Object.keys(statePaths).length}`);

const OUT = 'assets/ui/explore-native';
fs.mkdirSync(OUT, { recursive: true });

const write = (name, content) => fs.writeFileSync(path.join(OUT, name), content);
const svg = (w,h,body,viewBox=`0 0 ${w} ${h}`) => `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="${viewBox}">${body}</svg>`;

write('page-bg.svg', svg(1080,1495,`<defs><radialGradient id="r" cx="74%" cy="4%" r="55%"><stop stop-color="#c5ebe2"/><stop offset="1" stop-color="#d9eee7" stop-opacity="0"/></radialGradient><linearGradient id="g" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#d7efea"/><stop offset=".62" stop-color="#eef0df"/><stop offset="1" stop-color="#e6d9c2"/></linearGradient></defs><rect width="1080" height="1495" fill="url(#g)"/><rect width="1080" height="1495" fill="url(#r)"/>`));
write('header.svg', svg(1080,205,`<rect width="1080" height="205" fill="#073f49"/><rect y="201" width="1080" height="4" fill="#d1a947" fill-opacity=".5"/>`));
write('level-badge.svg', svg(130,108,`<rect x="3" y="3" width="124" height="102" rx="28" fill="#0a4b56" stroke="#d1a947" stroke-width="5"/>`));
write('coin-pill.svg', svg(245,92,`<rect x="3" y="3" width="239" height="86" rx="43" fill="#0a4b56" stroke="#d1a947" stroke-width="5"/><circle cx="47" cy="46" r="29" fill="#e9bb52" stroke="#fff0a4" stroke-width="3"/><text x="47" y="56" text-anchor="middle" font-family="Georgia,serif" font-size="30" font-weight="900" fill="#7b5417">F</text>`));
write('round.svg', svg(88,88,`<circle cx="44" cy="44" r="40" fill="#0a4b56" stroke="#d1a947" stroke-width="5"/>`));
write('count-card.svg', svg(120,94,`<rect x="2" y="2" width="116" height="90" rx="20" fill="#ffffff" fill-opacity=".58" stroke="#0e535b" stroke-opacity=".18" stroke-width="3"/>`));
write('map-frame.svg', svg(1040,885,`<rect x="3" y="3" width="1034" height="879" rx="35" fill="#b9dcd5" stroke="#315d60" stroke-width="6"/><rect x="12" y="12" width="1016" height="861" rx="27" fill="none" stroke="#f6efcf" stroke-opacity=".72" stroke-width="5"/><g stroke="#0d5158" stroke-opacity=".055" stroke-width="2">${Array.from({length:18},(_,i)=>`<path d="M${58*i} 14V871"/>`).join('')}${Array.from({length:16},(_,i)=>`<path d="M14 ${58*i}H1026"/>`).join('')}</g>`));
write('hint.svg', svg(275,64,`<rect x="2" y="2" width="271" height="60" rx="20" fill="#f7f1db" fill-opacity=".95" stroke="#194544" stroke-opacity=".18" stroke-width="2"/>`));
write('map-btn.svg', svg(82,82,`<rect x="3" y="3" width="76" height="76" rx="18" fill="#0a515b" stroke="#cda84f" stroke-width="5"/>`));
write('check-card.svg', svg(1040,270,`<rect x="2" y="2" width="1036" height="266" rx="32" fill="#fbf7e6" fill-opacity=".96" stroke="#0e4546" stroke-opacity=".18" stroke-width="3"/>`));
write('primary-btn.svg', svg(980,82,`<rect x="3" y="3" width="974" height="76" rx="20" fill="#0b4d56" stroke="#cfa94d" stroke-width="5"/>`));
write('nav-bg.svg', svg(1080,220,`<rect width="1080" height="220" fill="#073f49"/><rect width="1080" height="3" fill="#d8ae50" fill-opacity=".25"/>`));
write('nav-active.svg', svg(245,185,`<rect x="2" y="2" width="241" height="181" rx="28" fill="#116476"/>`));

const stateBody = Object.entries(statePaths).map(([code,d]) => `<path data-state="${code}" d="${d}" fill="#116b72" stroke="#d5aa4b" stroke-width="5" stroke-linejoin="round" fill-rule="evenodd"/>`).join('');
write('us-field-map.svg', svg(1000,620,stateBody,'0 0 1000 620'));
for (const [code,d] of Object.entries(statePaths)) {
  write(`state-${code}.svg`, svg(1000,620,`<path d="${d}" fill="#116b72" stroke="#d5aa4b" stroke-width="6" stroke-linejoin="round" fill-rule="evenodd"/>`,'0 0 1000 620'));
}

const resources = game.resources?.resources || (game.resources = { resources: [], resourceFolders: [] }).resources;
const ensureResource = (name,file) => {
  let r = resources.find(x => x.name === name);
  if (!r) {
    r = {alwaysLoaded:true,file,kind:'image',metadata:'{"extension":".svg"}',name,smoothed:true,userAdded:true};
    resources.push(r);
  } else Object.assign(r,{file,kind:'image',alwaysLoaded:true,smoothed:true});
};
const uiFiles = ['page-bg','header','level-badge','coin-pill','round','count-card','map-frame','hint','map-btn','check-card','primary-btn','nav-bg','nav-active','us-field-map'];
for (const n of uiFiles) ensureResource(`explore_${n.replaceAll('-','_')}`, `${OUT}/${n}.svg`);
for (const code of Object.keys(statePaths)) ensureResource(`state_${code}`, `${OUT}/state-${code}.svg`);

const uuid = () => crypto.randomUUID();
const sprite = (name,res) => ({assetStoreId:'',name,tags:'',type:'Sprite',updateIfNotVisible:false,variables:[],effects:[],behaviors:[],animations:[{name:'',useMultipleDirections:false,directions:[{looping:false,metadata:'',timeBetweenFrames:0.08,sprites:[{hasCustomCollisionMask:false,image:res,points:[],originPoint:{name:'origine',x:0,y:0},centerPoint:{automatic:true,name:'centre',x:0,y:0},customCollisionMask:[]}]}]}]});
const text = (name,str,size,color={r:23,g:63,b:68},bold=true,align='left') => ({assetStoreId:'',bold,italic:false,name,smoothed:true,tags:'',type:'TextObject::Text',underlined:false,variables:[],effects:[],behaviors:[],string:str,font:'',textAlignment:align,characterSize:size,color});
const inst = (name,x,y,w=0,h=0,layer='UI',z=1,custom=true) => ({angle:0,customSize:custom,height:h,layer,name,persistentUuid:uuid(),width:w,x,y,zOrder:z,numberProperties:[],stringProperties:[],initialVariables:[]});
const layers = ['Background','UI','Overlay'].map(name => ({ambientLightColorB:0,ambientLightColorG:8042920,ambientLightColorR:16,followBaseLayerCamera:false,isLightingLayer:false,name,visibility:true,cameras:[{defaultSize:true,defaultViewport:true,height:0,viewportBottom:1,viewportLeft:0,viewportRight:1,viewportTop:0,width:0}],effects:[]}));

const explore = game.layouts.find(l => l.name === 'Explore');
if (!explore) throw new Error('Explore layout missing');
explore.r = 233; explore.v = 230; explore.b = 211;
explore.layers = [{ambientLightColorB:0,ambientLightColorG:8042920,ambientLightColorR:16,followBaseLayerCamera:false,isLightingLayer:false,name:'',visibility:true,cameras:[{defaultSize:true,defaultViewport:true,height:0,viewportBottom:1,viewportLeft:0,viewportRight:1,viewportTop:0,width:0}],effects:[]},...layers];
explore.objects = [];
explore.instances = [];
explore.events = [];
explore.variables = [{folded:true,name:'MapScale',type:'number',value:1}];

const addSprite = (name,res,x,y,w,h,layer='UI',z=1) => { explore.objects.push(sprite(name,res)); explore.instances.push(inst(name,x,y,w,h,layer,z,true)); };
const addText = (name,str,size,x,y,color,bold=true,align='left',layer='UI',z=2) => { explore.objects.push(text(name,str,size,color,bold,align)); explore.instances.push(inst(name,x,y,0,0,layer,z,false)); };

addSprite('ExplorePageBg','explore_page_bg',0,205,1080,1495,'Background',0);
addSprite('TopHeader','explore_header',0,0,1080,205,'UI',1);
addSprite('LevelBadge','explore_level_badge',24,73,130,108,'UI',2);
addText('LevelText','LV 9',34,52,111,{r:245,g:234,b:210},true,'left','UI',3);
addText('BrandTitle','FOUND',50,282,74,{r:245,g:234,b:210},true,'left','UI',3);
addText('BrandSub','STAMP HUNT',22,302,139,{r:226,g:189,b:103},true,'left','UI',3);
addSprite('CoinPill','explore_coin_pill',626,77,245,92,'UI',2);
addText('CoinText','104.9K',42,705,100,{r:245,g:234,b:210},true,'left','UI',3);
addSprite('NoticeButton','explore_round',883,78,88,88,'UI',2);
addText('NoticeIcon','✉',34,910,104,{r:245,g:234,b:210},true,'left','UI',3);
addText('NoticeCount','2',22,943,70,{r:88,g:58,b:20},true,'left','Overlay',5);
addSprite('SettingsButton','explore_round',977,78,88,88,'UI',2);
addText('SettingsIcon','⚙',38,999,102,{r:245,g:234,b:210},true,'left','UI',3);

addText('ExploreEyebrow','FOUND NATIONAL FIELD MAP',24,28,267,{r:174,g:73,b:54},true,'left','UI',2);
addText('ExploreTitle','EXPLORE THE U.S.',55,28,304,{r:23,g:63,b:68},true,'left','UI',2);
addSprite('StateCountCard','explore_count_card',932,260,120,94,'UI',2);
addText('StateCount','50',38,970,274,{r:13,g:88,b:98},true,'left','UI',3);
addText('StateCountLabel','STATES',20,958,323,{r:110,g:126,b:121},true,'left','UI',3);

addSprite('MapFrame','explore_map_frame',20,390,1040,885,'UI',1);
addSprite('USMap','explore_us_field_map',120,585,820,508,'UI',2);
addSprite('DragHintBg','explore_hint',48,417,275,64,'UI',4);
addText('DragHint','drag, pinch or zoom',22,73,438,{r:139,g:74,b:55},true,'left','UI',5);
addSprite('ZoomIn','explore_map_btn',948,418,82,82,'UI',4);
addText('ZoomInText','+',40,977,436,{r:247,g:232,b:184},true,'left','UI',5);
addSprite('ZoomOut','explore_map_btn',948,510,82,82,'UI',4);
addText('ZoomOutText','−',40,978,529,{r:247,g:232,b:184},true,'left','UI',5);
addSprite('ResetMap','explore_map_btn',948,602,82,82,'UI',4);
addText('ResetText','RESET',18,961,632,{r:247,g:232,b:184},true,'left','UI',5);

addSprite('CheckCard','explore_check_card',20,1300,1040,270,'UI',1);
addText('DiscoveryLabel','LOCAL DISCOVERY',24,52,1333,{r:163,g:76,b:55},true,'left','UI',2);
addText('CheckTitle','U.S. CHECK-IN',42,52,1372,{r:23,g:63,b:68},true,'left','UI',2);
addText('CheckCopy','Use Check In to verify your current location and open your active state field map.',22,52,1429,{r:104,g:123,b:118},false,'left','UI',2);
addSprite('CheckInButton','explore_primary_btn',50,1472,980,82,'UI',2);
addText('CheckInText','CHECK IN',24,459,1499,{r:246,g:233,b:189},true,'left','UI',3);
addText('LegalText','Original digital souvenirs • Not valid postage • Not issued or endorsed by a postal authority',18,86,1602,{r:119,g:133,b:129},false,'left','UI',2);

addSprite('BottomNav','explore_nav_bg',0,1700,1080,220,'UI',1);
addSprite('ExploreActiveTile','explore_nav_active',14,1718,245,185,'UI',2);
const nav = [
  ['NavExploreIcon','◎',44,95,1748],['NavExploreLabel','EXPLORE',22,65,1822],
  ['NavCollectionIcon','▦',44,360,1748],['NavCollectionLabel','COLLECTION',22,317,1822],
  ['NavMailIcon','▤',44,628,1748],['NavMailLabel','MAILROOM',22,585,1822],
  ['NavPostIcon','▣',44,896,1748],['NavPostLabel','POST OFFICE',22,835,1822]
];
for (const [n,s,sz,x,y] of nav) addText(n,s,sz,x,y,{r: n.includes('Explore')?246:184,g:n.includes('Explore')?233:215,b:n.includes('Explore')?189:210},true,'left','UI',3);

const pathsLiteral = JSON.stringify(statePaths);
explore.events.push({disabled:false,folded:false,type:'BuiltinCommonInstructions::JsCode',inlineCode:[
  '/* __FOUND_EXPLORE_NATIVE_RUNTIME__ */',
  `const STATE_PATHS=${pathsLiteral};`,
  `const STATE_NAMES=${JSON.stringify({AL:'Alabama',AK:'Alaska',AZ:'Arizona',AR:'Arkansas',CA:'California',CO:'Colorado',CT:'Connecticut',DE:'Delaware',FL:'Florida',GA:'Georgia',HI:'Hawaii',ID:'Idaho',IL:'Illinois',IN:'Indiana',IA:'Iowa',KS:'Kansas',KY:'Kentucky',LA:'Louisiana',ME:'Maine',MD:'Maryland',MA:'Massachusetts',MI:'Michigan',MN:'Minnesota',MS:'Mississippi',MO:'Missouri',MT:'Montana',NE:'Nebraska',NV:'Nevada',NH:'New Hampshire',NJ:'New Jersey',NM:'New Mexico',NY:'New York',NC:'North Carolina',ND:'North Dakota',OH:'Ohio',OK:'Oklahoma',OR:'Oregon',PA:'Pennsylvania',RI:'Rhode Island',SC:'South Carolina',SD:'South Dakota',TN:'Tennessee',TX:'Texas',UT:'Utah',VT:'Vermont',VA:'Virginia',WA:'Washington',WV:'West Virginia',WI:'Wisconsin',WY:'Wyoming'})};`,
  'window.__FOUND_EXPLORE_NATIVE_SCENE__=runtimeScene;',
  'if(!window.__FOUND_EXPLORE_NATIVE_BOUND__){',
  ' window.__FOUND_EXPLORE_NATIVE_BOUND__=true;',
  ' const st={p:new Map(),drag:false,moved:false,last:null,base:null,scale:1}; window.__FOUND_EXPLORE_NATIVE_STATE__=st;',
  ' const gamePoint=(ev,rs)=>{const c=document.querySelector("canvas"); if(!c)return null; const r=c.getBoundingClientRect(); const g=rs.getGame(); return {x:(ev.clientX-r.left)/r.width*g.getGameResolutionWidth(),y:(ev.clientY-r.top)/r.height*g.getGameResolutionHeight()};};',
  ' const hit=(o,p)=>o&&p&&p.x>=o.getX()&&p.x<=o.getX()+o.getWidth()&&p.y>=o.getY()&&p.y<=o.getY()+o.getHeight();',
  ' const mapObj=(rs)=>rs.getObjects("USMap")[0];',
  ' const frameObj=(rs)=>rs.getObjects("MapFrame")[0];',
  ' const reset=(rs)=>{const m=mapObj(rs);if(!m)return;m.setX(120);m.setY(585);m.setWidth(820);m.setHeight(508);st.scale=1;};',
  ' const zoom=(rs,f,cx,cy)=>{const m=mapObj(rs);if(!m)return;const ns=Math.max(.9,Math.min(1.32,st.scale*f));f=ns/st.scale;st.scale=ns;const ox=(cx-m.getX())/m.getWidth(),oy=(cy-m.getY())/m.getHeight();const nw=m.getWidth()*f,nh=m.getHeight()*f;m.setX(cx-ox*nw);m.setY(cy-oy*nh);m.setWidth(nw);m.setHeight(nh);};',
  ' const hitState=(rs,p)=>{const m=mapObj(rs);if(!m)return null;const lx=(p.x-m.getX())/m.getWidth()*1000,ly=(p.y-m.getY())/m.getHeight()*620;if(lx<0||ly<0||lx>1000||ly>620)return null;const c=document.createElement("canvas").getContext("2d");for(const [code,d] of Object.entries(STATE_PATHS)){try{if(c.isPointInPath(new Path2D(d),lx,ly,"evenodd"))return code;}catch(e){}}return null;};',
  ' const go=(scene)=>{const rs=window.__FOUND_EXPLORE_NATIVE_SCENE__;if(rs)gdjs.evtTools.runtimeScene.replaceScene(rs,scene,false);};',
  ' document.addEventListener("pointerdown",ev=>{const rs=window.__FOUND_EXPLORE_NATIVE_SCENE__;if(!rs)return;const p=gamePoint(ev,rs);const fr=frameObj(rs);if(!hit(fr,p))return;st.p.set(ev.pointerId,p);st.last=p;st.moved=false;st.drag=true;});',
  ' document.addEventListener("pointermove",ev=>{const rs=window.__FOUND_EXPLORE_NATIVE_SCENE__;if(!rs||!st.p.has(ev.pointerId))return;const p=gamePoint(ev,rs);const old=st.p.get(ev.pointerId);st.p.set(ev.pointerId,p);const pts=[...st.p.values()];if(pts.length===1&&st.drag){const m=mapObj(rs);const dx=p.x-old.x,dy=p.y-old.y;if(Math.abs(dx)+Math.abs(dy)>4)st.moved=true;m.setX(m.getX()+dx);m.setY(m.getY()+dy);} });',
  ' document.addEventListener("pointerup",ev=>{const rs=window.__FOUND_EXPLORE_NATIVE_SCENE__;if(!rs)return;const p=gamePoint(ev,rs);st.p.delete(ev.pointerId);const zin=rs.getObjects("ZoomIn")[0],zout=rs.getObjects("ZoomOut")[0],resetBtn=rs.getObjects("ResetMap")[0],check=rs.getObjects("CheckInButton")[0];if(hit(zin,p)){const m=mapObj(rs);zoom(rs,1.16,m.getX()+m.getWidth()/2,m.getY()+m.getHeight()/2);return;}if(hit(zout,p)){const m=mapObj(rs);zoom(rs,.86,m.getX()+m.getWidth()/2,m.getY()+m.getHeight()/2);return;}if(hit(resetBtn,p)){reset(rs);return;}if(hit(check,p)){runtimeScene.getGame().getVariables().get("SelectedStateCode").setString("NY");runtimeScene.getGame().getVariables().get("SelectedStateName").setString("New York");go("StateFieldMap");return;} if(!st.moved){const code=hitState(rs,p);if(code){runtimeScene.getGame().getVariables().get("SelectedStateCode").setString(code);runtimeScene.getGame().getVariables().get("SelectedStateName").setString(STATE_NAMES[code]||code);go("StateFieldMap");return;}} st.drag=false;st.last=null;});',
  ' document.addEventListener("click",ev=>{const rs=window.__FOUND_EXPLORE_NATIVE_SCENE__;if(!rs)return;const p=gamePoint(ev,rs);if(p.y<1700)return; if(p.x<270)return; if(p.x<540){go("Collection");return;} if(p.x<810){go("MailroomRush");return;} go("PostalExchange");});',
  '}',
  'const level=runtimeScene.getObjects("LevelText")[0],coins=runtimeScene.getObjects("CoinText")[0],notice=runtimeScene.getObjects("NoticeCount")[0];',
  'try{const s=JSON.parse(localStorage.getItem("found.state.v1")||"null");if(s){if(level&&Number.isFinite(Number(s.level)))level.setString("LV "+Number(s.level));const c=Number(s.coins??s.wallet??0);if(coins&&Number.isFinite(c))coins.setString(c>=1000?(c/1000).toFixed(c>=100000?1:0)+"K":String(c));if(notice&&Number.isFinite(Number(s.unreadNotices)))notice.setString(String(s.unreadNotices));}}catch(e){}'
],parameterObjects:'',useStrict:false,eventsSheetExpanded:false});

// Native state field map scene. This is intentionally editor-visible too; it is the next page to polish.
let stateScene = game.layouts.find(l => l.name === 'StateFieldMap');
if (!stateScene) { stateScene = {b:211,disableInputWhenNotFocused:true,mangledName:'StateFieldMap',name:'StateFieldMap',r:233,standardSortMethod:true,stopSoundsOnStartup:true,title:'',v:230,uiSettings:{grid:false,gridType:'rectangular',gridWidth:32,gridHeight:32,gridOffsetX:0,gridOffsetY:0,gridColor:10401023,gridAlpha:.8,snap:false,zoomFactor:.55,windowMask:true},objectsGroups:[],variables:[],instances:[],objects:[],events:[],layers:explore.layers,behaviorsSharedData:[]}; game.layouts.push(stateScene); }
stateScene.objects=[]; stateScene.instances=[]; stateScene.events=[]; stateScene.layers=explore.layers;
const sAddSprite=(name,res,x,y,w,h,z=1)=>{stateScene.objects.push(sprite(name,res));stateScene.instances.push(inst(name,x,y,w,h,'UI',z,true));};
const sAddText=(name,str,size,x,y,color,bold=true,z=2)=>{stateScene.objects.push(text(name,str,size,color,bold,'left'));stateScene.instances.push(inst(name,x,y,0,0,'UI',z,false));};
sAddSprite('StatePageBg','explore_page_bg',0,205,1080,1495,0);sAddSprite('StateTopHeader','explore_header',0,0,1080,205,1);sAddText('StateBack','← U.S. MAP',26,35,91,{r:246,g:233,b:189},true,3);sAddText('StateEyebrow','FOUND STATE FIELD MAP',22,290,70,{r:174,g:73,b:54},true,3);sAddText('StateTitle','NEW YORK',50,290,105,{r:245,g:234,b:210},true,3);sAddSprite('StateMapFrame','explore_map_frame',20,270,1040,1180,1);
const stateObj = sprite('StateShape','state_NY'); stateObj.animations=[]; const codes=Object.keys(statePaths); for(const code of codes){stateObj.animations.push({name:code,useMultipleDirections:false,directions:[{looping:false,metadata:'',timeBetweenFrames:.08,sprites:[{hasCustomCollisionMask:false,image:`state_${code}`,points:[],originPoint:{name:'origine',x:0,y:0},centerPoint:{automatic:true,name:'centre',x:0,y:0},customCollisionMask:[]}]}]});} stateScene.objects.push(stateObj); stateScene.instances.push(inst('StateShape',90,500,900,558,'UI',2,true));sAddText('StateLegend','Live destination zones are numbered on the map',24,60,315,{r:95,g:116,b:112},true,4);sAddText('StateStatus','STATE FIELD MAP',24,770,90,{r:226,g:189,b:103},true,3);
stateScene.events.push({disabled:false,folded:false,type:'BuiltinCommonInstructions::JsCode',inlineCode:[
  '/* __FOUND_STATE_FIELD_NATIVE__ */','const game=runtimeScene.getGame();const code=game.getVariables().get("SelectedStateCode").getAsString()||"NY";const name=game.getVariables().get("SelectedStateName").getAsString()||"New York";const shape=runtimeScene.getObjects("StateShape")[0];const title=runtimeScene.getObjects("StateTitle")[0];if(title)title.setString(name.toUpperCase());const codes='+JSON.stringify(codes)+';const idx=Math.max(0,codes.indexOf(code));if(shape)shape.setAnimation(idx);window.__FOUND_STATE_SCENE__=runtimeScene;if(!window.__FOUND_STATE_BACK_BOUND__){window.__FOUND_STATE_BACK_BOUND__=true;document.addEventListener("click",ev=>{const rs=window.__FOUND_STATE_SCENE__;if(!rs)return;const c=document.querySelector("canvas");if(!c)return;const r=c.getBoundingClientRect();const g=rs.getGame();const x=(ev.clientX-r.left)/r.width*g.getGameResolutionWidth(),y=(ev.clientY-r.top)/r.height*g.getGameResolutionHeight();if(x<260&&y<205)gdjs.evtTools.runtimeScene.replaceScene(rs,"Explore",false);});}'
],parameterObjects:'',useStrict:false,eventsSheetExpanded:false});

const ensureGlobal = (name,type,value) => { if(!game.variables.some(v=>v.name===name)) game.variables.push({folded:true,name,type,value}); };
ensureGlobal('SelectedStateCode','string','NY'); ensureGlobal('SelectedStateName','string','New York');
game.properties.version='0.3.1';
fs.writeFileSync(projectPath, JSON.stringify(game));
console.log('Patched Explore as native editor-visible GDevelop objects (v0.3.1).');
