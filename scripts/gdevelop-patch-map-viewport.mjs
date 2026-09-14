import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

const projectPath = process.argv[2] || 'game.json';
const game = JSON.parse(fs.readFileSync(projectPath, 'utf8'));
const statePaths = JSON.parse(fs.readFileSync('gdevelop-source/us-state-paths.json', 'utf8'));
const OUT = 'assets/ui/explore-native';
fs.mkdirSync(OUT, { recursive: true });

// A real sprite mask is more reliable in GDevelop/Pixi than constructing a Graphics
// mask from JavaScript. It is kept behind the visible frame in the editor and used as
// the runtime alpha mask for the map content.
fs.writeFileSync(path.join(OUT, 'map-clip-mask.svg'), '<svg xmlns="http://www.w3.org/2000/svg" width="1040" height="885" viewBox="0 0 1040 885"><rect x="0" y="0" width="1040" height="885" rx="26" fill="#fff"/></svg>');

const resources = game.resources?.resources || (game.resources = { resources: [], resourceFolders: [] }).resources;
let maskRes = resources.find(r => r.name === 'explore_map_clip_mask');
if (!maskRes) {
  maskRes = {alwaysLoaded:true,file:`${OUT}/map-clip-mask.svg`,kind:'image',metadata:'{"extension":".svg"}',name:'explore_map_clip_mask',smoothed:true,userAdded:true};
  resources.push(maskRes);
} else {
  Object.assign(maskRes,{file:`${OUT}/map-clip-mask.svg`,kind:'image',alwaysLoaded:true,smoothed:true});
}

const sprite = (name,res) => ({assetStoreId:'',name,tags:'',type:'Sprite',updateIfNotVisible:false,variables:[],effects:[],behaviors:[],animations:[{name:'',useMultipleDirections:false,directions:[{looping:false,metadata:'',timeBetweenFrames:.08,sprites:[{hasCustomCollisionMask:false,image:res,points:[],originPoint:{name:'origine',x:0,y:0},centerPoint:{automatic:true,name:'centre',x:0,y:0},customCollisionMask:[]}]}]}]});
const instance = (name,x,y,w,h,layer='UI',z=0) => ({angle:0,customSize:true,height:h,layer,name,persistentUuid:crypto.randomUUID(),width:w,x,y,zOrder:z,numberProperties:[],stringProperties:[],initialVariables:[]});
const ensureMaskObject = (layout, name, x, y, w, h) => {
  if (!layout.objects.some(o => o.name === name)) layout.objects.push(sprite(name,'explore_map_clip_mask'));
  if (!layout.instances.some(i => i.name === name)) layout.instances.push(instance(name,x,y,w,h,'UI',0));
};

const exploreLayout = game.layouts.find(l => l.name === 'Explore');
const stateLayout = game.layouts.find(l => l.name === 'StateFieldMap');
if (!exploreLayout || !stateLayout) throw new Error('Explore or StateFieldMap layout missing');
ensureMaskObject(exploreLayout,'MapClipMask',34,404,1012,857);
ensureMaskObject(stateLayout,'StateMapClipMask',34,364,1012,1292);

// Parse the SVG M/L/Z paths once at build time. Android WebViews are inconsistent
// about Path2D(SVG-string), so taps use deterministic polygon hit testing instead.
function parsePolygons(d) {
  const tokens = String(d).match(/[MLZ]|-?\d+(?:\.\d+)?/g) || [];
  const polys = [];
  let poly = [];
  for (let i = 0; i < tokens.length;) {
    const t = tokens[i++];
    if (t === 'M' || t === 'L') {
      const x = Number(tokens[i++]);
      const y = Number(tokens[i++]);
      if (t === 'M' && poly.length) { polys.push(poly); poly = []; }
      poly.push([x,y]);
    } else if (t === 'Z') {
      if (poly.length) { polys.push(poly); poly = []; }
    }
  }
  if (poly.length) polys.push(poly);
  return polys.filter(p => p.length >= 3);
}
const STATE_HITS = {};
for (const [code,d] of Object.entries(statePaths)) {
  const p = parsePolygons(d);
  const pts = p.flat();
  const xs = pts.map(v=>v[0]), ys = pts.map(v=>v[1]);
  const minX=Math.min(...xs), maxX=Math.max(...xs), minY=Math.min(...ys), maxY=Math.max(...ys);
  STATE_HITS[code] = {p,b:[minX,minY,maxX,maxY],c:[(minX+maxX)/2,(minY+maxY)/2]};
}

const findRuntime = (layoutName, marker) => {
  const layout = game.layouts.find(l => l.name === layoutName);
  if (!layout) throw new Error(`${layoutName} layout missing`);
  const event = (layout.events || []).find(e => e.type === 'BuiltinCommonInstructions::JsCode' && Array.isArray(e.inlineCode) && e.inlineCode.some(line => line.includes(marker)));
  if (!event) throw new Error(`${marker} runtime missing`);
  return event.inlineCode;
};
const replaceLine = (lines, needle, replacement) => {
  const i = lines.findIndex(line => line.includes(needle));
  if (i < 0) throw new Error(`Line not found: ${needle}`);
  lines[i] = replacement;
  return i;
};
const replaceBoundBlock = (lines, marker, replacementLines) => {
  const start = lines.findIndex(line => line.includes(marker));
  if (start < 0) throw new Error(`Bound block not found: ${marker}`);
  let end = start + 1;
  while (end < lines.length && String(lines[end]).trim() !== '}') end++;
  if (end >= lines.length) throw new Error(`Bound block end not found: ${marker}`);
  lines.splice(start, end - start + 1, ...replacementLines);
};

const explore = findRuntime('Explore', '__FOUND_EXPLORE_RESPONSIVE_RUNTIME__');
const namesIndex = explore.findIndex(line => line.includes('const STATE_NAMES='));
if (namesIndex < 0) throw new Error('STATE_NAMES line missing');
if (!explore.some(line => line.includes('const STATE_HITS='))) explore.splice(namesIndex + 1, 0, `const STATE_HITS=${JSON.stringify(STATE_HITS)};`);

replaceLine(explore, 'const mapObj=rs=>rs.getObjects("USMap")[0]',
' const mapObj=rs=>rs.getObjects("USMap")[0],frameObj=rs=>rs.getObjects("MapFrame")[0],maskObj=rs=>rs.getObjects("MapClipMask")[0]; const inner=rs=>{const fr=frameObj(rs),sx=rs.getGame().getGameResolutionWidth()/1080,p=14*sx;return fr?{x:fr.getX()+p,y:fr.getY()+p,w:Math.max(1,fr.getWidth()-p*2),h:Math.max(1,fr.getHeight()-p*2)}:null;}; const applyMask=rs=>{const m=mapObj(rs),mk=maskObj(rs),box=inner(rs);if(!m||!mk||!box)return;mk.setX(box.x);mk.setY(box.y);mk.setWidth(box.w);mk.setHeight(box.h);try{const ro=m._renderer?.getRendererObject?.(),mo=mk._renderer?.getRendererObject?.();if(ro&&mo&&ro.mask!==mo)ro.mask=mo;}catch(e){}}; const contain=rs=>{const m=mapObj(rs),box=inner(rs);if(!m||!box)return;if(m.getWidth()<=box.w)m.setX(box.x+(box.w-m.getWidth())/2);else m.setX(Math.max(box.x+box.w-m.getWidth(),Math.min(box.x,m.getX())));if(m.getHeight()<=box.h)m.setY(box.y+(box.h-m.getHeight())/2);else m.setY(Math.max(box.y+box.h-m.getHeight(),Math.min(box.y,m.getY())));applyMask(rs);};');
replaceLine(explore, 'const reset=rs=>{const m=mapObj(rs),fr=frameObj(rs);',
' const reset=rs=>{const m=mapObj(rs),fr=frameObj(rs);if(!m||!fr)return;const bw=Math.min(rs.getGame().getGameResolutionWidth()/1080*820,fr.getWidth()-rs.getGame().getGameResolutionWidth()/1080*180),bh=bw*620/1000;m.setWidth(bw);m.setHeight(bh);m.setX(fr.getX()+(fr.getWidth()-bw)/2);m.setY(fr.getY()+(fr.getHeight()-bh)/2);st.scale=1;contain(rs);};');
replaceLine(explore, 'const zoom=(rs,f,cx,cy)=>{const m=mapObj(rs),fr=frameObj(rs);',
' const zoom=(rs,f,cx,cy)=>{const m=mapObj(rs),fr=frameObj(rs);if(!m||!fr)return;const ns=Math.max(.85,Math.min(4,st.scale*f));f=ns/st.scale;if(Math.abs(f-1)<.0001)return;st.scale=ns;const ox=(cx-m.getX())/m.getWidth(),oy=(cy-m.getY())/m.getHeight(),nw=m.getWidth()*f,nh=m.getHeight()*f;m.setX(cx-ox*nw);m.setY(cy-oy*nh);m.setWidth(nw);m.setHeight(nh);contain(rs);};');
replaceLine(explore, 'const hitState=(rs,p)=>{const m=mapObj(rs);',
' const pointIn=(poly,x,y)=>{let inside=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const xi=poly[i][0],yi=poly[i][1],xj=poly[j][0],yj=poly[j][1];if(((yi>y)!=(yj>y))&&(x<(xj-xi)*(y-yi)/((yj-yi)||1e-9)+xi))inside=!inside;}return inside;}; const hitState=(rs,p)=>{const m=mapObj(rs),box=inner(rs);if(!m||!box||!p||p.x<box.x||p.x>box.x+box.w||p.y<box.y||p.y>box.y+box.h)return null;const lx=(p.x-m.getX())/m.getWidth()*1000,ly=(p.y-m.getY())/m.getHeight()*620;if(lx<0||ly<0||lx>1000||ly>620)return null;for(const [code,h] of Object.entries(STATE_HITS)){let inside=false;for(const poly of h.p)if(pointIn(poly,lx,ly))inside=!inside;if(inside)return code;}const tx=30/m.getWidth()*1000,ty=30/m.getHeight()*620;let best=null,bestD=Infinity;for(const [code,h] of Object.entries(STATE_HITS)){const [x1,y1,x2,y2]=h.b;if(lx<x1-tx||lx>x2+tx||ly<y1-ty||ly>y2+ty)continue;const dx=(lx-h.c[0])/tx,dy=(ly-h.c[1])/ty,d=dx*dx+dy*dy;if(d<bestD){bestD=d;best=code;}}return best;};');

replaceBoundBlock(explore, 'if(!window.__FOUND_EXPLORE_RESPONSIVE_BOUND__){', [
'if(!window.__FOUND_EXPLORE_RESPONSIVE_BOUND__){',
' window.__FOUND_EXPLORE_RESPONSIVE_BOUND__=true;',
' const st={p:new Map(),moved:false,pinch:0,scale:1,down:null};window.__FOUND_EXPLORE_RESPONSIVE_STATE__=st;',
' const rsNow=()=>window.__FOUND_EXPLORE_NATIVE_SCENE__;',
' const gamePoint=(ev,rs)=>{const c=document.querySelector("canvas");if(!c)return null;const r=c.getBoundingClientRect(),g=rs.getGame();return{x:(ev.clientX-r.left)/r.width*g.getGameResolutionWidth(),y:(ev.clientY-r.top)/r.height*g.getGameResolutionHeight()};};',
' const hit=(o,p)=>o&&p&&p.x>=o.getX()&&p.x<=o.getX()+o.getWidth()&&p.y>=o.getY()&&p.y<=o.getY()+o.getHeight();',
' const go=(rs,scene)=>{if(rs)gdjs.evtTools.runtimeScene.replaceScene(rs,scene,false);};',
' const canvas=document.querySelector("canvas");if(canvas)canvas.style.touchAction="none";',
' document.addEventListener("pointerdown",ev=>{const rs=rsNow();if(!rs)return;const p=gamePoint(ev,rs),fr=frameObj(rs);if(!hit(fr,p))return;const controls=[rs.getObjects("ZoomIn")[0],rs.getObjects("ZoomOut")[0],rs.getObjects("ResetMap")[0]];if(controls.some(o=>hit(o,p)))return;st.p.set(ev.pointerId,p);st.moved=false;st.down={id:ev.pointerId,x:p.x,y:p.y,code:hitState(rs,p)};if(st.p.size===2){const a=[...st.p.values()];st.pinch=Math.hypot(a[0].x-a[1].x,a[0].y-a[1].y);}});',
' document.addEventListener("pointermove",ev=>{const rs=rsNow();if(!rs||!st.p.has(ev.pointerId))return;const p=gamePoint(ev,rs),old=st.p.get(ev.pointerId);st.p.set(ev.pointerId,p);const pts=[...st.p.values()];if(pts.length===1){const m=mapObj(rs),dx=p.x-old.x,dy=p.y-old.y;if(st.down&&Math.hypot(p.x-st.down.x,p.y-st.down.y)>12)st.moved=true;if(m&&st.moved){m.setX(m.getX()+dx);m.setY(m.getY()+dy);contain(rs);}}else if(pts.length>=2){const dist=Math.hypot(pts[0].x-pts[1].x,pts[0].y-pts[1].y),mid={x:(pts[0].x+pts[1].x)/2,y:(pts[0].y+pts[1].y)/2};if(st.pinch>0)zoom(rs,dist/st.pinch,mid.x,mid.y);st.pinch=dist;st.moved=true;}});',
' document.addEventListener("pointerup",ev=>{const rs=rsNow();if(!rs)return;const p=gamePoint(ev,rs),down=st.down;st.p.delete(ev.pointerId);if(st.p.size<2)st.pinch=0;const zin=rs.getObjects("ZoomIn")[0],zout=rs.getObjects("ZoomOut")[0],rb=rs.getObjects("ResetMap")[0],check=rs.getObjects("CheckInButton")[0];if(hit(zin,p)){const m=mapObj(rs);zoom(rs,1.35,m.getX()+m.getWidth()/2,m.getY()+m.getHeight()/2);st.down=null;return;}if(hit(zout,p)){const m=mapObj(rs);zoom(rs,1/1.35,m.getX()+m.getWidth()/2,m.getY()+m.getHeight()/2);st.down=null;return;}if(hit(rb,p)){reset(rs);st.down=null;return;}if(hit(check,p)){rs.getGame().getVariables().get("SelectedStateCode").setString("NY");rs.getGame().getVariables().get("SelectedStateName").setString("New York");st.down=null;go(rs,"StateFieldMap");return;}if(down&&down.id===ev.pointerId&&!st.moved){const code=hitState(rs,p)||down.code;if(code){rs.getGame().getVariables().get("SelectedStateCode").setString(code);rs.getGame().getVariables().get("SelectedStateName").setString(STATE_NAMES[code]||code);st.down=null;go(rs,"StateFieldMap");return;}}st.down=null;if(!st.p.size)st.moved=false;});',
' document.addEventListener("pointercancel",ev=>{st.p.delete(ev.pointerId);if(st.p.size<2)st.pinch=0;if(!st.p.size){st.moved=false;st.down=null;}});',
' document.addEventListener("click",ev=>{const rs=rsNow();if(!rs)return;const p=gamePoint(ev,rs),H=rs.getGame().getGameResolutionHeight(),navY=H-220;if(!p||p.y<navY)return;const W=rs.getGame().getGameResolutionWidth();if(p.x<W*.25)return;if(p.x<W*.5){go(rs,"Collection");return;}if(p.x<W*.75){go(rs,"MailroomRush");return;}go(rs,"PostalExchange");});',
'}'
]);

// Keep the mask and pan limits authoritative every frame, not just during a gesture.
const tailIndex = explore.findIndex(line => line.includes('const level=obj("LevelText")'));
if (tailIndex >= 0 && !explore.some(line => line.includes('__FOUND_EXPLORE_FRAME_GUARD__'))) {
  explore.splice(tailIndex,0,'/* __FOUND_EXPLORE_FRAME_GUARD__ */ try{contain(runtimeScene);}catch(e){}');
}

// State map: use the same native sprite-mask strategy and strict pan containment.
const state = findRuntime('StateFieldMap', '__FOUND_STATE_RESPONSIVE_RUNTIME__');
const stateBoundStart = state.findIndex(line => line.includes('if(!window.__FOUND_STATE_RESPONSIVE_BOUND__){'));
if (stateBoundStart < 0) throw new Error('State bound block missing');
let stateBoundEnd = stateBoundStart + 1;
while (stateBoundEnd < state.length && !String(state[stateBoundEnd]).trim().endsWith('}')) stateBoundEnd++;
// The original state listener block is one long line in current generated projects.
if (stateBoundEnd >= state.length) stateBoundEnd = stateBoundStart;
state.splice(stateBoundStart, stateBoundEnd-stateBoundStart+1,
'if(!window.__FOUND_STATE_RESPONSIVE_BOUND__){window.__FOUND_STATE_RESPONSIVE_BOUND__=true;const st={p:new Map(),pinch:0,moved:false,scale:1},rsNow=()=>window.__FOUND_STATE_NATIVE_SCENE__,gp=(ev,rs)=>{const c=document.querySelector("canvas");if(!c)return null;const r=c.getBoundingClientRect(),g=rs.getGame();return{x:(ev.clientX-r.left)/r.width*g.getGameResolutionWidth(),y:(ev.clientY-r.top)/r.height*g.getGameResolutionHeight()};},hit=(q,p)=>q&&p&&p.x>=q.getX()&&p.x<=q.getX()+q.getWidth()&&p.y>=q.getY()&&p.y<=q.getY()+q.getHeight(),go=(rs,s)=>gdjs.evtTools.runtimeScene.replaceScene(rs,s,false),inner=rs=>{const fr=rs.getObjects("StateMapFrame")[0],sx=rs.getGame().getGameResolutionWidth()/1080,p=14*sx;return fr?{x:fr.getX()+p,y:fr.getY()+p,w:Math.max(1,fr.getWidth()-p*2),h:Math.max(1,fr.getHeight()-p*2)}:null;},clip=rs=>{const sh=rs.getObjects("StateShape")[0],mk=rs.getObjects("StateMapClipMask")[0],box=inner(rs);if(!sh||!mk||!box)return;mk.setX(box.x);mk.setY(box.y);mk.setWidth(box.w);mk.setHeight(box.h);if(sh.getWidth()<=box.w)sh.setX(box.x+(box.w-sh.getWidth())/2);else sh.setX(Math.max(box.x+box.w-sh.getWidth(),Math.min(box.x,sh.getX())));if(sh.getHeight()<=box.h)sh.setY(box.y+(box.h-sh.getHeight())/2);else sh.setY(Math.max(box.y+box.h-sh.getHeight(),Math.min(box.y,sh.getY())));try{const ro=sh._renderer?.getRendererObject?.(),mo=mk._renderer?.getRendererObject?.();if(ro&&mo&&ro.mask!==mo)ro.mask=mo;}catch(e){};},reset=rs=>{const sh=rs.getObjects("StateShape")[0],fr=rs.getObjects("StateMapFrame")[0],cd=rs.getGame().getVariables().get("SelectedStateCode").getAsString()||"NY",b=STATE_BOUNDS[cd]||{aspect:1.6};if(!sh||!fr)return;let bw=fr.getWidth()*.62,bh=bw/b.aspect,maxH=fr.getHeight()*.55;if(bh>maxH){bh=maxH;bw=bh*b.aspect;}sh.setWidth(bw);sh.setHeight(bh);sh.setX(fr.getX()+(fr.getWidth()-bw)/2);sh.setY(fr.getY()+(fr.getHeight()-bh)/2);st.scale=1;clip(rs);},zoom=(rs,f,cx,cy)=>{const sh=rs.getObjects("StateShape")[0];if(!sh)return;const ns=Math.max(.8,Math.min(4,st.scale*f));f=ns/st.scale;st.scale=ns;const ox=(cx-sh.getX())/sh.getWidth(),oy=(cy-sh.getY())/sh.getHeight(),nw=sh.getWidth()*f,nh=sh.getHeight()*f;sh.setX(cx-ox*nw);sh.setY(cy-oy*nh);sh.setWidth(nw);sh.setHeight(nh);clip(rs);};document.addEventListener("pointerdown",ev=>{const rs=rsNow();if(!rs)return;const p=gp(ev,rs),fr=rs.getObjects("StateMapFrame")[0];if(!hit(fr,p))return;st.p.set(ev.pointerId,p);st.moved=false;if(st.p.size===2){const a=[...st.p.values()];st.pinch=Math.hypot(a[0].x-a[1].x,a[0].y-a[1].y);}});document.addEventListener("pointermove",ev=>{const rs=rsNow();if(!rs||!st.p.has(ev.pointerId))return;const p=gp(ev,rs),old=st.p.get(ev.pointerId);st.p.set(ev.pointerId,p);const a=[...st.p.values()];if(a.length===1){const sh=rs.getObjects("StateShape")[0],dx=p.x-old.x,dy=p.y-old.y;if(Math.abs(dx)+Math.abs(dy)>6)st.moved=true;if(sh&&st.moved){sh.setX(sh.getX()+dx);sh.setY(sh.getY()+dy);clip(rs);}}else if(a.length>=2){const d=Math.hypot(a[0].x-a[1].x,a[0].y-a[1].y),mx=(a[0].x+a[1].x)/2,my=(a[0].y+a[1].y)/2;if(st.pinch>0)zoom(rs,d/st.pinch,mx,my);st.pinch=d;st.moved=true;}});document.addEventListener("pointerup",ev=>{const rs=rsNow();if(!rs)return;const p=gp(ev,rs);st.p.delete(ev.pointerId);if(st.p.size<2)st.pinch=0;const zi=rs.getObjects("StateZoomIn")[0],zo=rs.getObjects("StateZoomOut")[0],rb=rs.getObjects("StateReset")[0],back=rs.getObjects("StateBackBg")[0],sh=rs.getObjects("StateShape")[0];if(hit(back,p)){go(rs,"Explore");return;}if(hit(zi,p)){zoom(rs,1.35,sh.getX()+sh.getWidth()/2,sh.getY()+sh.getHeight()/2);return;}if(hit(zo,p)){zoom(rs,1/1.35,sh.getX()+sh.getWidth()/2,sh.getY()+sh.getHeight()/2);return;}if(hit(rb,p)){reset(rs);return;}if(!st.p.size)st.moved=false;});document.addEventListener("pointercancel",ev=>{st.p.delete(ev.pointerId);if(st.p.size<2)st.pinch=0;});document.addEventListener("click",ev=>{const rs=rsNow();if(!rs)return;const p=gp(ev,rs),H=rs.getGame().getGameResolutionHeight(),navY=H-220;if(!p||p.y<navY)return;const W=rs.getGame().getGameResolutionWidth();if(p.x<W*.25){go(rs,"Explore");return;}if(p.x<W*.5){go(rs,"Collection");return;}if(p.x<W*.75){go(rs,"MailroomRush");return;}go(rs,"PostalExchange");});window.__FOUND_STATE_FRAME_CLIP__=clip;}');

const stateTail = state.findIndex(line => line.includes('const lv=o("StateLevelText")'));
if (stateTail >= 0 && !state.some(line => line.includes('__FOUND_STATE_FRAME_GUARD__'))) state.splice(stateTail,0,'/* __FOUND_STATE_FRAME_GUARD__ */ try{window.__FOUND_STATE_FRAME_CLIP__?.(runtimeScene);}catch(e){}');

game.properties.version = '0.3.4';
fs.writeFileSync(projectPath, JSON.stringify(game));
console.log('Patched strict framed map clipping and deterministic 50-state tap routing (v0.3.4).');
