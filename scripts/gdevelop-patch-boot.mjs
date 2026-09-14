import fs from 'node:fs';

const projectPath = process.argv[2] || 'game.json';
const game = JSON.parse(fs.readFileSync(projectPath, 'utf8'));

const ensureResource = (name, file) => {
  const resources = game.resources?.resources || (game.resources = { resources: [], resourceFolders: [] }).resources;
  let resource = resources.find((entry) => entry.name === name);
  if (!resource) {
    resource = {
      alwaysLoaded: true,
      file,
      kind: 'image',
      metadata: '{"extension":".svg"}',
      name,
      smoothed: true,
      userAdded: true
    };
    resources.push(resource);
  } else {
    resource.file = file;
    resource.kind = 'image';
    resource.alwaysLoaded = true;
    resource.smoothed = true;
  }
};

ensureResource('ui_splash_track', 'assets/ui/splash-track.svg');
ensureResource('ui_splash_bar', 'assets/ui/splash-bar.svg');

const boot = game.layouts.find((layout) => layout.name === 'Boot');
if (!boot) throw new Error('Boot layout not found');

// Keep the exact FOUND splash palette.
boot.r = 8;
boot.v = 58;
boot.b = 67;

const byName = (items, name) => items.find((item) => item.name === name);
const emblemInstance = byName(boot.instances, 'SplashEmblem');
const wordmarkInstance = byName(boot.instances, 'StartupWordmark');
const trackInstance = byName(boot.instances, 'LoadingTrack');
const barInstance = byName(boot.instances, 'LoadingBar');

if (!emblemInstance || !wordmarkInstance || !trackInstance || !barInstance) {
  throw new Error('One or more Boot splash instances are missing');
}

// 1080 x 1920 design space. The full splash stack is centered as a group,
// matching the current app: emblem, FOUND wordmark, then the thin loading scan.
Object.assign(emblemInstance, {
  x: 375,
  y: 743,
  width: 330,
  height: 310,
  customSize: true,
  layer: 'Content'
});
Object.assign(wordmarkInstance, {
  x: 430,
  y: 1068,
  customSize: false,
  layer: 'Content'
});
Object.assign(trackInstance, {
  x: 429,
  y: 1175,
  width: 222,
  height: 6,
  customSize: true,
  layer: 'Content'
});
Object.assign(barInstance, {
  x: 429,
  y: 1175,
  width: 102,
  height: 6,
  customSize: true,
  layer: 'Content'
});

const splashEmblem = byName(boot.objects, 'SplashEmblem');
const wordmark = byName(boot.objects, 'StartupWordmark');
const loadingTrack = byName(boot.objects, 'LoadingTrack');
const loadingBar = byName(boot.objects, 'LoadingBar');

if (wordmark) {
  wordmark.string = 'FOUND';
  wordmark.bold = true;
  wordmark.characterSize = 51;
  wordmark.textAlignment = 'center';
  wordmark.color = { r: 245, g: 234, b: 210 };
}

const setSpriteImage = (object, image) => {
  const sprite = object?.animations?.[0]?.directions?.[0]?.sprites?.[0];
  if (!sprite) throw new Error(`Sprite definition missing for ${object?.name || image}`);
  sprite.image = image;
};

setSpriteImage(splashEmblem, 'ui_splash_emblem');
setSpriteImage(loadingTrack, 'ui_splash_track');
setSpriteImage(loadingBar, 'ui_splash_bar');

// Remove an older copy of this runtime centering/animation event before adding it again.
boot.events = (boot.events || []).filter((event) =>
  !(event.type === 'BuiltinCommonInstructions::JsCode' &&
    Array.isArray(event.inlineCode) &&
    event.inlineCode.some((line) => line.includes('__FOUND_BOOT_SPLASH_MOTION__')))
);

boot.events.push({
  disabled: false,
  folded: false,
  type: 'BuiltinCommonInstructions::JsCode',
  inlineCode: [
    '/* __FOUND_BOOT_SPLASH_MOTION__ */',
    'const cx = 540;',
    'const emblem = runtimeScene.getObjects("SplashEmblem")[0];',
    'const wordmark = runtimeScene.getObjects("StartupWordmark")[0];',
    'const track = runtimeScene.getObjects("LoadingTrack")[0];',
    'const bar = runtimeScene.getObjects("LoadingBar")[0];',
    'if (emblem) emblem.setX(cx - emblem.getWidth() / 2);',
    'if (wordmark) wordmark.setX(cx - wordmark.getWidth() / 2);',
    'if (track) track.setX(cx - track.getWidth() / 2);',
    'if (track && bar) {',
    '  const seconds = runtimeScene.getTimeManager().getTimeFromStart() / 1000;',
    '  const cycle = seconds % 2;',
    '  const u = cycle <= 1 ? cycle : 2 - cycle;',
    '  const eased = 0.5 - 0.5 * Math.cos(Math.PI * u);',
    '  const left = track.getX();',
    '  const travel = Math.max(0, track.getWidth() - bar.getWidth());',
    '  bar.setX(left + travel * eased);',
    '  bar.setY(track.getY());',
    '}'
  ],
  parameterObjects: '',
  useStrict: false,
  eventsSheetExpanded: false
});

game.properties.version = '0.2.1';
fs.writeFileSync(projectPath, JSON.stringify(game));
console.log('Patched Boot splash: centered stack + animated left/right loading scan.');
