import fs from 'node:fs';

const projectPath = process.argv[2] || 'game.json';
const game = JSON.parse(fs.readFileSync(projectPath, 'utf8'));
const paths = JSON.parse(fs.readFileSync('gdevelop-source/us-state-paths.json', 'utf8'));

if (Object.keys(paths).length !== 50) throw new Error(`Expected 50 state paths, got ${Object.keys(paths).length}`);

const explore = game.layouts.find((layout) => layout.name === 'Explore');
if (!explore) throw new Error('Explore layout not found');

function foundExploreRuntime() {
  const existing = document.getElementById('foundGdExplore');
  if (existing) existing.remove();

  const STATES = [
    ['AL','Alabama',32.8,-86.8],['AK','Alaska',64.2,-152.3],['AZ','Arizona',34.3,-111.7],['AR','Arkansas',34.9,-92.4],
    ['CA','California',37.2,-119.7],['CO','Colorado',39.0,-105.5],['CT','Connecticut',41.6,-72.7],['DE','Delaware',39.0,-75.5],
    ['FL','Florida',28.6,-82.4],['GA','Georgia',32.7,-83.3],['HI','Hawaii',20.9,-157.5],['ID','Idaho',44.2,-114.5],
    ['IL','Illinois',40.0,-89.2],['IN','Indiana',39.9,-86.3],['IA','Iowa',42.1,-93.5],['KS','Kansas',38.5,-98.3],
    ['KY','Kentucky',37.5,-85.3],['LA','Louisiana',31.0,-92.0],['ME','Maine',45.3,-69.0],['MD','Maryland',39.0,-76.7],
    ['MA','Massachusetts',42.3,-71.8],['MI','Michigan',44.3,-85.6],['MN','Minnesota',46.3,-94.3],['MS','Mississippi',32.7,-89.7],
    ['MO','Missouri',38.5,-92.5],['MT','Montana',47.0,-109.6],['NE','Nebraska',41.5,-99.8],['NV','Nevada',39.3,-116.6],
    ['NH','New Hampshire',43.7,-71.6],['NJ','New Jersey',40.1,-74.7],['NM','New Mexico',34.4,-106.1],['NY','New York',42.9,-75.5],
    ['NC','North Carolina',35.5,-79.4],['ND','North Dakota',47.5,-100.5],['OH','Ohio',40.3,-82.8],['OK','Oklahoma',35.6,-97.5],
    ['OR','Oregon',44.0,-120.6],['PA','Pennsylvania',40.9,-77.8],['RI','Rhode Island',41.7,-71.6],['SC','South Carolina',33.8,-80.9],
    ['SD','South Dakota',44.4,-100.2],['TN','Tennessee',35.8,-86.4],['TX','Texas',31.5,-99.3],['UT','Utah',39.3,-111.7],
    ['VT','Vermont',44.1,-72.7],['VA','Virginia',37.5,-78.8],['WA','Washington',47.4,-120.7],['WV','West Virginia',38.6,-80.6],
    ['WI','Wisconsin',44.6,-89.7],['WY','Wyoming',43.0,-107.6]
  ].map(([code,name,lat,lon]) => ({code,name,lat,lon}));
  const STATE_BY_CODE = new Map(STATES.map((s) => [s.code, s]));
  const esc = (value) => String(value ?? '').replace(/[&<>"']/g, (c) => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

  const root = document.createElement('div');
  root.id = 'foundGdExplore';
  root.innerHTML = `<style>
    #foundGdExplore{position:fixed;inset:0;z-index:90000;display:grid;grid-template-rows:96px minmax(0,1fr) 116px;overflow:hidden;background:#e9e6d3;color:#173f44;font-family:Arial,system-ui,sans-serif;-webkit-tap-highlight-color:transparent;box-sizing:border-box}
    #foundGdExplore *{box-sizing:border-box}
    #foundGdExplore button{font:inherit;color:inherit;-webkit-tap-highlight-color:transparent}
    #foundGdExplore .gdHeader{display:grid;grid-template-columns:72px minmax(0,1fr) auto;align-items:center;gap:10px;padding:12px 15px;background:#073f49;color:#f8eed8;border-bottom:1px solid rgba(216,174,80,.48)}
    #foundGdExplore .level{width:62px;height:58px;display:grid;place-items:center;border:2px solid #d1a947;border-radius:17px;color:#f7ecd5;font:900 17px/1 Georgia,serif;background:#0a4b56}
    #foundGdExplore .brand{text-align:center;line-height:1;min-width:0}
    #foundGdExplore .brand b{display:block;font:900 24px/1 Georgia,serif;letter-spacing:.23em;text-indent:.23em;white-space:nowrap}
    #foundGdExplore .brand small{display:block;margin-top:8px;color:#e2bd67;font-size:9px;font-weight:900;letter-spacing:.24em;white-space:nowrap}
    #foundGdExplore .headActions{display:flex;align-items:center;gap:7px}
    #foundGdExplore .coin{height:50px;min-width:126px;display:flex;align-items:center;justify-content:center;gap:8px;padding:0 12px;border:2px solid #d1a947;border-radius:25px;background:#0a4b56;color:#f8eed8;font-weight:900;font-size:18px}
    #foundGdExplore .coin i{width:31px;height:31px;display:grid;place-items:center;border-radius:50%;background:radial-gradient(circle at 35% 30%,#fff1a2,#e7b23d 52%,#9c6515);border:1px solid #fff0a4;color:#7b5417;font:900 16px/1 Georgia,serif;font-style:normal}
    #foundGdExplore .round{position:relative;width:50px;height:50px;padding:0;border:2px solid #d1a947;border-radius:50%;background:#0a4b56;color:#f8eed8;font-size:23px;line-height:1}
    #foundGdExplore .noticeBadge{position:absolute;right:-2px;top:-8px;min-width:23px;height:23px;padding:0 5px;display:grid;place-items:center;border-radius:12px;background:#e6bb59;color:#5d3d14;font-size:11px;font-weight:900}
    #foundGdExplore .main{min-height:0;overflow:hidden;background:radial-gradient(circle at 74% 4%,rgba(197,235,226,.9),transparent 32%),linear-gradient(180deg,#d7efea 0%,#eef0df 60%,#e6d9c2 100%)}
    #foundGdExplore .page{height:100%;min-height:0;display:grid;grid-template-rows:72px minmax(0,1fr) 142px 24px;gap:7px;padding:8px 13px 4px}
    #foundGdExplore .page[hidden]{display:none!important}
    #foundGdExplore .pageHead{display:flex;align-items:flex-end;justify-content:space-between;gap:10px;padding:0 4px}
    #foundGdExplore .eyebrow{display:block;color:#ae4936;font-size:10px;font-weight:900;letter-spacing:.14em}
    #foundGdExplore h1{margin:4px 0 0;color:#173f44;font:900 31px/.95 Georgia,serif;letter-spacing:.01em}
    #foundGdExplore .stateCount{flex:0 0 auto;min-width:70px;padding:8px 10px;border:1px solid rgba(14,83,91,.18);border-radius:12px;background:rgba(255,255,255,.55);text-align:center}
    #foundGdExplore .stateCount b{display:block;color:#0d5862;font:900 22px/1 Georgia,serif}
    #foundGdExplore .stateCount small{display:block;margin-top:3px;color:#6e7e79;font-size:9px;font-weight:900;letter-spacing:.08em}
    #foundGdExplore .mapFrame{position:relative;min-height:0;overflow:hidden;border:3px solid #315d60;border-radius:25px;background:#b9dcd5;box-shadow:inset 0 0 0 4px rgba(246,239,207,.72),0 6px 0 rgba(24,58,56,.13)}
    #foundGdExplore .mapFrame:after{content:'';position:absolute;inset:0;pointer-events:none;background:linear-gradient(rgba(13,81,88,.065) 1px,transparent 1px),linear-gradient(90deg,rgba(13,81,88,.065) 1px,transparent 1px);background-size:58px 58px;mix-blend-mode:multiply}
    #foundGdExplore .mapHint{position:absolute;z-index:8;left:18px;top:18px;padding:9px 14px;border:1px solid rgba(25,69,68,.18);border-radius:12px;background:rgba(247,241,219,.93);color:#8b4a37;font-size:11px;font-weight:900;letter-spacing:.06em;box-shadow:0 2px 7px rgba(24,58,56,.09);pointer-events:none}
    #foundGdExplore .mapControls{position:absolute;z-index:9;right:16px;top:17px;display:grid;gap:8px}
    #foundGdExplore .mapControls button{width:52px;height:52px;display:grid;place-items:center;padding:0;border:2px solid #cda84f;border-radius:12px;background:linear-gradient(#17616a,#0a4650);color:#f7e8b8;box-shadow:0 3px 0 rgba(30,45,40,.17);font:900 23px/1 Georgia,serif}
    #foundGdExplore .mapControls button:last-child{font-size:11px}
    #foundGdExplore svg.mapSvg{position:relative;z-index:2;width:100%;height:100%;display:block;touch-action:none;user-select:none}
    #foundGdExplore .statePath{fill:#116b72;stroke:#d5aa4b;stroke-width:5;stroke-linejoin:round;vector-effect:non-scaling-stroke;cursor:pointer;transition:filter .12s ease,fill .12s ease}
    #foundGdExplore .statePath:active{fill:#d5aa4b;stroke:#0c5660}
    #foundGdExplore .checkCard{display:grid;grid-template-rows:auto auto;gap:10px;padding:14px 20px;border:1px solid rgba(14,69,70,.18);border-radius:21px;background:rgba(251,247,230,.94);box-shadow:0 4px 12px rgba(35,65,58,.08)}
    #foundGdExplore .checkCopy span{display:block;color:#a34c37;font-size:10px;font-weight:900;letter-spacing:.12em}
    #foundGdExplore .checkCopy b{display:block;margin-top:6px;color:#173f44;font:900 23px/1 Georgia,serif}
    #foundGdExplore .checkCopy small{display:block;margin-top:7px;color:#687b76;font-size:11px;line-height:1.3}
    #foundGdExplore .checkButton{width:100%;height:50px;border:2px solid #cfa94d;border-radius:12px;background:linear-gradient(#17616a 0%,#0e4b53 54%,#08383f 100%);color:#f6e9bd;font-size:11px;font-weight:900;letter-spacing:.08em;box-shadow:inset 0 -3px 0 rgba(0,24,28,.34),0 2px 0 rgba(72,57,24,.2)}
    #foundGdExplore .legal{display:flex;align-items:center;justify-content:center;color:#778581;text-align:center;font-size:9px;white-space:nowrap}
    #foundGdExplore .gdNav{display:grid;grid-template-columns:repeat(4,1fr);gap:5px;padding:10px 14px 12px;background:#073f49;border-top:1px solid rgba(216,174,80,.3)}
    #foundGdExplore .navBtn{border:0;border-radius:17px;background:transparent;color:#b8d7d2;display:grid;place-items:center;align-content:center;gap:8px;font-size:10px;font-weight:900;letter-spacing:.07em}
    #foundGdExplore .navBtn i{font-style:normal;font-size:28px;line-height:1}
    #foundGdExplore .navBtn.active{background:#116476;color:#f6e9bd}
    #foundGdExplore .statePage{height:100%;min-height:0;display:grid;grid-template-rows:65px minmax(0,1fr) 24px;gap:7px;padding:8px 13px 4px}
    #foundGdExplore .stateHeader{display:grid;grid-template-columns:auto minmax(0,1fr) auto;gap:9px;align-items:center;padding:0 4px}
    #foundGdExplore .backBtn{height:38px;padding:0 12px;border:2px solid #cda84f;border-radius:10px;background:linear-gradient(#17616a,#0a4650);color:#f6e9bd;font-size:9px;font-weight:900;letter-spacing:.06em}
    #foundGdExplore .stateHeading{min-width:0}
    #foundGdExplore .stateHeading h1{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:25px}
    #foundGdExplore .stateStatus{padding:8px 9px;border:1px solid rgba(20,78,75,.19);border-radius:9px;background:rgba(255,255,255,.55);color:#0f5c64;font-size:9px;font-weight:900;letter-spacing:.06em;white-space:nowrap}
    #foundGdExplore .stateLegend{position:absolute;z-index:8;left:18px;top:18px;max-width:72%;padding:9px 12px;border:1px solid rgba(25,69,68,.18);border-radius:10px;background:rgba(247,241,219,.93);color:#5f7470;font-size:10px;font-weight:900;letter-spacing:.04em;pointer-events:none}
    #foundGdExplore .stateLegend b{color:#0d5a63}
    #foundGdExplore .zone{fill:#0d6370;stroke:#f0ce74;stroke-width:3;vector-effect:non-scaling-stroke}
    #foundGdExplore .zoneText{fill:#fff3c7;font:900 11px Arial,sans-serif;text-anchor:middle;dominant-baseline:central;pointer-events:none}
    @media(max-width:430px){#foundGdExplore{grid-template-rows:88px minmax(0,1fr) 108px}#foundGdExplore .gdHeader{grid-template-columns:58px minmax(0,1fr) auto;padding:10px 9px;gap:7px}#foundGdExplore .level{width:54px;height:54px;font-size:15px}#foundGdExplore .brand b{font-size:21px}#foundGdExplore .brand small{font-size:7px;margin-top:6px}#foundGdExplore .coin{min-width:105px;height:45px;padding:0 8px;font-size:15px}.headActions{gap:4px!important}#foundGdExplore .coin i{width:27px;height:27px;font-size:14px}#foundGdExplore .round{width:44px;height:44px;font-size:20px}#foundGdExplore .page{grid-template-rows:64px minmax(0,1fr) 132px 22px;padding-left:8px;padding-right:8px}#foundGdExplore h1{font-size:27px}#foundGdExplore .mapControls{right:10px;top:12px}#foundGdExplore .mapControls button{width:45px;height:45px}#foundGdExplore .mapHint{left:12px;top:12px}#foundGdExplore .gdNav{padding-left:8px;padding-right:8px}}
    @media(max-height:760px){#foundGdExplore{grid-template-rows:80px minmax(0,1fr) 94px}#foundGdExplore .page{grid-template-rows:56px minmax(0,1fr) 104px 18px;gap:4px;padding-top:4px}#foundGdExplore .checkCard{padding:8px 12px;gap:5px}#foundGdExplore .checkCopy b{font-size:18px}#foundGdExplore .checkCopy small{font-size:9px;margin-top:3px}#foundGdExplore .checkButton{height:38px}#foundGdExplore .legal{font-size:7px}#foundGdExplore .statePage{grid-template-rows:54px minmax(0,1fr) 18px;gap:4px;padding-top:4px}}
  </style>
  <header class="gdHeader">
    <div class="level">LV&nbsp; 1</div>
    <div class="brand"><b>FOUND</b><small>STAMP HUNT</small></div>
    <div class="headActions"><div class="coin"><i>F</i><span>0</span></div><button class="round" type="button" aria-label="Collector notices">✉<span class="noticeBadge">0</span></button><button class="round" type="button" aria-label="Settings">⚙</button></div>
  </header>
  <main class="main">
    <section class="page" id="gdNationPage">
      <div class="pageHead"><div><span class="eyebrow">FOUND NATIONAL FIELD MAP</span><h1>EXPLORE THE U.S.</h1></div><div class="stateCount"><b>50</b><small>STATES</small></div></div>
      <div class="mapFrame" id="gdNationFrame">
        <div class="mapHint">drag, pinch or zoom</div>
        <div class="mapControls"><button type="button" data-map="nation" data-action="in">+</button><button type="button" data-map="nation" data-action="out">−</button><button type="button" data-map="nation" data-action="reset">RESET</button></div>
        <svg class="mapSvg" id="gdNationSvg" viewBox="60 25 800 570" aria-label="Interactive United States field map"><g id="gdNationGroup"></g></svg>
      </div>
      <div class="checkCard"><div class="checkCopy"><span>LOCAL DISCOVERY</span><b>U.S. CHECK-IN</b><small id="gdCheckMessage">Use Check In to verify your current location and open your active state field map.</small></div><button class="checkButton" id="gdCheckIn" type="button">CHECK IN</button></div>
      <div class="legal">Original digital souvenirs • Not valid postage • Not issued or endorsed by a postal authority</div>
    </section>
    <section class="statePage" id="gdStatePage" hidden>
      <div class="stateHeader"><button class="backBtn" id="gdStateBack" type="button">← U.S. MAP</button><div class="stateHeading"><span class="eyebrow" id="gdStateEyebrow">FOUND STATE FIELD MAP</span><h1 id="gdStateTitle">NEW YORK</h1></div><span class="stateStatus" id="gdStateStatus">10 LIVE ZONES</span></div>
      <div class="mapFrame"><div class="stateLegend" id="gdStateLegend"></div><div class="mapControls"><button type="button" data-map="state" data-action="in">+</button><button type="button" data-map="state" data-action="out">−</button><button type="button" data-map="state" data-action="reset">RESET</button></div><svg class="mapSvg" id="gdStateSvg" viewBox="0 0 1000 620" aria-label="State field map"><g id="gdStateGroup"></g></svg></div>
      <div class="legal">Original digital souvenirs • Not valid postage • Not issued or endorsed by a postal authority</div>
    </section>
  </main>
  <nav class="gdNav"><button class="navBtn active" type="button"><i>⌖</i>EXPLORE</button><button class="navBtn" type="button" data-nav="1"><i>▦</i>COLLECTION</button><button class="navBtn" type="button" data-nav="2"><i>▤</i>MAILROOM</button><button class="navBtn" type="button" data-nav="3"><i>▣</i>POST OFFICE</button></nav>`;
  document.body.appendChild(root);

  const svgNS = 'http://www.w3.org/2000/svg';
  const nationGroup = root.querySelector('#gdNationGroup');
  STATES.forEach((state) => {
    const d = PATHS[state.code];
    if (!d) return;
    const path = document.createElementNS(svgNS, 'path');
    path.setAttribute('d', d);
    path.setAttribute('class', 'statePath');
    path.setAttribute('data-state', state.code);
    path.setAttribute('fill-rule', 'evenodd');
    path.setAttribute('aria-label', `Open ${state.name} state field map`);
    nationGroup.appendChild(path);
  });

  function controller(svg, initial) {
    let base = initial.slice();
    let view = initial.slice();
    let dragging = false;
    let moved = false;
    let px = 0, py = 0;
    const pointers = new Map();
    let pinch = 0;
    const apply = () => svg.setAttribute('viewBox', view.join(' '));
    const zoom = (factor, cx=.5, cy=.5) => {
      const nw = Math.max(base[2]/5, Math.min(base[2]*1.08, view[2]/factor));
      const nh = Math.max(base[3]/5, Math.min(base[3]*1.08, view[3]/factor));
      const ax = view[0] + view[2]*cx, ay = view[1] + view[3]*cy;
      view[0] = ax - nw*cx; view[1] = ay - nh*cy; view[2] = nw; view[3] = nh; apply(); moved = true;
    };
    svg.addEventListener('pointerdown', (e) => { svg.setPointerCapture?.(e.pointerId); pointers.set(e.pointerId,{x:e.clientX,y:e.clientY}); dragging=true; moved=false; px=e.clientX;py=e.clientY; });
    svg.addEventListener('pointermove', (e) => { if(!pointers.has(e.pointerId)) return; e.preventDefault(); pointers.set(e.pointerId,{x:e.clientX,y:e.clientY}); const pts=[...pointers.values()]; const rect=svg.getBoundingClientRect(); if(pts.length===1&&dragging){const dx=e.clientX-px,dy=e.clientY-py;if(Math.abs(dx)+Math.abs(dy)>4)moved=true;view[0]-=dx*(view[2]/rect.width);view[1]-=dy*(view[3]/rect.height);px=e.clientX;py=e.clientY;apply();} else if(pts.length>=2){const dist=Math.hypot(pts[0].x-pts[1].x,pts[0].y-pts[1].y);if(pinch)zoom(dist/pinch,.5,.5);pinch=dist;} });
    const end=(e)=>{pointers.delete(e.pointerId);dragging=false;pinch=0;}; svg.addEventListener('pointerup',end);svg.addEventListener('pointercancel',end);
    return { zoomIn:()=>zoom(1.35), zoomOut:()=>zoom(.74), reset:()=>{view=base.slice();apply();moved=false;}, setBase:(next)=>{base=next.slice();view=next.slice();apply();}, wasMoved:()=>moved };
  }

  const nationSvg = root.querySelector('#gdNationSvg');
  const nationCtl = controller(nationSvg, [60,25,800,570]);
  let stateCtl = null;
  let selected = null;

  function addNewYorkZones(group, bbox) {
    const spots = [[.17,.60],[.27,.46],[.38,.67],[.47,.38],[.56,.55],[.65,.29],[.72,.47],[.79,.66],[.84,.36],[.91,.54]];
    spots.forEach(([fx,fy], index) => {
      const g=document.createElementNS(svgNS,'g');
      const c=document.createElementNS(svgNS,'circle');
      const t=document.createElementNS(svgNS,'text');
      c.setAttribute('cx',bbox.x+bbox.width*fx);c.setAttribute('cy',bbox.y+bbox.height*fy);c.setAttribute('r',Math.max(3,Math.min(7,bbox.width*.018)));c.setAttribute('class','zone');
      t.setAttribute('x',bbox.x+bbox.width*fx);t.setAttribute('y',bbox.y+bbox.height*fy+.5);t.setAttribute('class','zoneText');t.textContent=String(index+1);
      g.append(c,t);group.appendChild(g);
    });
  }

  function openState(code) {
    const state = STATE_BY_CODE.get(code); if(!state) return;
    selected = state;
    root.querySelector('#gdNationPage').hidden = true;
    root.querySelector('#gdStatePage').hidden = false;
    root.querySelector('#gdStateEyebrow').textContent = `FOUND STATE FIELD MAP • ${state.code}`;
    root.querySelector('#gdStateTitle').textContent = state.name.toUpperCase();
    root.querySelector('#gdStateStatus').textContent = state.code === 'NY' ? '10 LIVE ZONES' : 'STATE READY';
    root.querySelector('#gdStateLegend').innerHTML = state.code === 'NY' ? `<b>NEW YORK</b> • live destination zones are numbered on the field map` : `<b>${esc(state.name.toUpperCase())}</b> • state field map ready • destination zones coming with this state set`;
    const group = root.querySelector('#gdStateGroup'); group.replaceChildren();
    const p=document.createElementNS(svgNS,'path'); p.setAttribute('d',PATHS[state.code]);p.setAttribute('class','statePath');p.setAttribute('fill-rule','evenodd');group.appendChild(p);
    requestAnimationFrame(()=>{const b=p.getBBox();const pad=Math.max(18,Math.max(b.width,b.height)*.25);const box=[b.x-pad,b.y-pad,b.width+pad*2,b.height+pad*2];const svg=root.querySelector('#gdStateSvg');svg.setAttribute('viewBox',box.join(' '));stateCtl=controller(svg,box);if(state.code==='NY')addNewYorkZones(group,b);});
  }
  function showNation(){selected=null;root.querySelector('#gdStatePage').hidden=true;root.querySelector('#gdNationPage').hidden=false;nationCtl.reset();}

  nationGroup.addEventListener('click',(e)=>{const p=e.target.closest('[data-state]');if(p&&!nationCtl.wasMoved())openState(p.dataset.state);});
  root.querySelector('#gdStateBack').onclick=showNation;
  root.querySelectorAll('[data-map]').forEach((button)=>button.addEventListener('click',()=>{const ctl=button.dataset.map==='nation'?nationCtl:stateCtl;if(!ctl)return;if(button.dataset.action==='in')ctl.zoomIn();else if(button.dataset.action==='out')ctl.zoomOut();else ctl.reset();}));
  root.querySelectorAll('[data-nav]').forEach((button)=>button.addEventListener('click',()=>{root.remove();runtimeScene.getVariables().get('NavigateTo').setNumber(Number(button.dataset.nav));}));

  root.querySelector('#gdCheckIn').addEventListener('click',()=>{
    const msg=root.querySelector('#gdCheckMessage');const button=root.querySelector('#gdCheckIn');
    if(!navigator.geolocation){msg.textContent='Location services are not available on this device.';return;}
    button.disabled=true;button.textContent='CHECKING…';msg.textContent='Verifying your current U.S. location…';
    navigator.geolocation.getCurrentPosition((pos)=>{
      const lat=pos.coords.latitude,lon=pos.coords.longitude;
      let best=null,bestScore=Infinity;
      for(const s of STATES){const dx=(lon-s.lon)*Math.cos(lat*Math.PI/180),dy=lat-s.lat;const score=dx*dx+dy*dy;if(score<bestScore){bestScore=score;best=s;}}
      button.disabled=false;button.textContent='CHECK IN';
      if(best){msg.textContent=`Location verified • opening ${best.name} field map.`;setTimeout(()=>openState(best.code),220);}else msg.textContent='FOUND could not verify your state. Try again with device location enabled.';
    },()=>{button.disabled=false;button.textContent='CHECK IN';msg.textContent='FOUND could not access your location. Enable location permission and try again.';},{enableHighAccuracy:true,timeout:12000,maximumAge:60000});
  });

  window.FOUND_GD_EXPLORE = { openState, showNation };
}

explore.objects = [];
explore.instances = [];
explore.objectsGroups = [];
explore.variables = [{ folded:true, name:'NavigateTo', type:'number', value:0 }];
explore.events = [
  {
    disabled:false, folded:false, type:'BuiltinCommonInstructions::Standard',
    conditions:[{type:{inverted:false,value:'SceneJustBegins'},parameters:[''],subInstructions:[]}], actions:[],
    events:[{
      disabled:false, folded:false, type:'BuiltinCommonInstructions::JsCode',
      inlineCode:[`const PATHS=${JSON.stringify(paths)};`, `(${foundExploreRuntime.toString()})();`],
      parameterObjects:'', useStrict:false, eventsSheetExpanded:false
    }]
  },
  {
    disabled:false,folded:false,type:'BuiltinCommonInstructions::Standard',
    conditions:[{type:{inverted:false,value:'VarScene'},parameters:['NavigateTo','=','1'],subInstructions:[]}],
    actions:[{type:{inverted:false,value:'Scene'},parameters:['','"Collection"',''],subInstructions:[]}],events:[]
  },
  {
    disabled:false,folded:false,type:'BuiltinCommonInstructions::Standard',
    conditions:[{type:{inverted:false,value:'VarScene'},parameters:['NavigateTo','=','2'],subInstructions:[]}],
    actions:[{type:{inverted:false,value:'Scene'},parameters:['','"MailroomRush"',''],subInstructions:[]}],events:[]
  },
  {
    disabled:false,folded:false,type:'BuiltinCommonInstructions::Standard',
    conditions:[{type:{inverted:false,value:'VarScene'},parameters:['NavigateTo','=','3'],subInstructions:[]}],
    actions:[{type:{inverted:false,value:'Scene'},parameters:['','"PostalExchange"',''],subInstructions:[]}],events:[]
  }
];

explore.r = 217;
explore.v = 234;
explore.b = 232;
game.properties.version = '0.3.0';
fs.writeFileSync(projectPath, JSON.stringify(game));
console.log('Patched Explore: current nationwide field map, 50 interactive state shapes, state field maps, pan/zoom and check-in UI.');
