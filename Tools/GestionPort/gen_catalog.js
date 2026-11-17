global.window=global; global.THREE=require('three');
for (const f of ['core','terrain','data']) require(require('path').resolve(__dirname,'../..')+'/Web/js/'+f+'.js');
const D=SS.Data, fs=require('fs');
const q=s=>'"'+String(s).replace(/\\/g,'\\\\').replace(/"/g,'\\"')+'"';
const num=v=>{ if(v===undefined||v===null) return '0'; const s=String(v); return /[.eE]/.test(s)? s : s; };
const val=(v,t)=> t==='string'? (v===undefined?'null':q(v)) : t==='bool'? (v?'true':'false') : t==='double'? (v===undefined?'0':String(Number(v))) : t==='int'? (v===undefined?'0':String(v|0)) : '';
function cls(name, fields){ return `    public sealed class ${name}\n    {\n${fields.map(f=>`        public ${f[1]} ${f[0]};`).join('\n')}\n    }\n`; }
function arr(type, list, fields){ return list.map(o=>`            new ${type} { ${fields.filter(f=>o[f[0]]!==undefined).map(f=>f[0]+' = '+val(o[f[0]],f[1])).join(', ')} }`).join(',\n'); }
const LF=[['id','string'],['name','string'],['kind','string'],['tier','int'],['seats','int'],['maxLen','double'],['speed','double'],['spacing','double'],['cost','double'],['pylonH','double'],['comfort','double'],['wind','double'],['bubble','bool'],['big','bool']];
const BF=[['id','string'],['name','string'],['cat','string'],['type','string'],['cost','double'],['upkeep','double'],['cap','double'],['price','double'],['dur','double'],['quality','double'],['r','double'],['look','string'],['road','bool'],['alt','bool'],['eve','bool'],['research','string'],['prestige','double'],['attract','double'],['fun','double'],['tags','string'],['summer','double'],['seg','int'],['stars','int'],['hb','bool'],['desc','string']];
// seg absent => -1 (pas un hébergement)
D.BUILDINGS.forEach(b=>{ if(b.seg===undefined) b.seg=-1; if(b.price===undefined) b._noPrice=true; });
const RF=[['cat','string'],['id','string'],['name','string'],['cost','double'],['days','int'],['desc','string'],['tier','int'],['req','string']];
const WF=[['id','string'],['name','string'],['icon','string'],['demand','double'],['sky','int'],['snow','double'],['light','double'],['warm','double'],['wind','double'],['rain','bool']];
const PF=[['id','string'],['name','string'],['w','double'],['spend','double'],['tags','string'],['child','bool']];
const LoF=[['id','string'],['name','string'],['amount','double'],['days','int'],['rate','double']];
const W=Object.keys(D.WEATHER).map(k=>Object.assign({id:k},D.WEATHER[k]));
let cs=`// FICHIER GÉNÉRÉ depuis Web/js/data.js (script gen_catalog.js) : ne pas modifier à la main.
// Catalogues de la gestion : remontées, bâtiments, recherche, météo, profils de visiteurs, prêts, noms.
namespace SkiStation.Gestion
{
${cls('LiftDef',LF)}${cls('BuildingDef',BF.concat([['hasPrice','bool']]))}${cls('ResearchDef',RF)}${cls('WeatherDef',WF)}${cls('ProfileDef',PF.concat([['skill','double[]']]))}${cls('LoanDef',LoF)}
    public static partial class Catalog
    {
        public static readonly string[] DIFF_NAMES = { ${D.DIFF.map(d=>q(d.name)).join(', ')} };
        public static readonly string[] DIFF_COLORS = { ${D.DIFF.map(d=>q(d.color)).join(', ')} };
        public static readonly string[] SKILL = { ${D.SKILL.map(q).join(', ')} };
        public static readonly LiftDef[] LIFTS = {
${arr('LiftDef',D.LIFTS,LF)}
        };
        public static readonly BuildingDef[] BUILDINGS = {
${D.BUILDINGS.map(o=>`            new BuildingDef { ${BF.filter(f=>o[f[0]]!==undefined).map(f=>f[0]+' = '+val(o[f[0]],f[1])).join(', ')}, hasPrice = ${o._noPrice?'false':'true'} }`).join(',\n')}
        };
        public static readonly ResearchDef[] RESEARCH = {
${arr('ResearchDef',D.RESEARCH,RF)}
        };
        public static readonly string[] RCATS = { ${D.RCATS.map(q).join(', ')} };
        public static readonly string[] SEG = { ${D.SEG.map(q).join(', ')} };
        public static readonly string[] RANGE = { ${D.RANGE.map(q).join(', ')} };
        public static readonly string[] STAFF = { ${D.STAFF.map(q).join(', ')} };
        public static readonly LoanDef[] LOANS = {
${arr('LoanDef',D.LOANS,LoF)}
        };
        public static readonly WeatherDef[] WEATHER = {
${arr('WeatherDef',W,WF)}
        };
        public static readonly ProfileDef[] PROFILES = {
${D.PROFILES.map(o=>`            new ProfileDef { ${PF.filter(f=>o[f[0]]!==undefined).map(f=>f[0]+' = '+val(o[f[0]],f[1])).join(', ')}, skill = new double[] { ${o.skill.join(', ')} } }`).join(',\n')}
        };
        public static readonly string[] MEDALS = { ${D.MEDALS.map(q).join(', ')} };
        public static readonly string[] FIRST = { ${D.FIRST.map(q).join(', ')} };
        public static readonly string[] LAST = { ${D.LAST.map(q).join(', ')} };
        public static readonly string[] PISTE_NAMES = { ${D.PISTE_NAMES.map(q).join(', ')} };
        public static readonly string[] LIFT_NAMES = { ${D.LIFT_NAMES.map(q).join(', ')} };
    }
}
`;
fs.writeFileSync(require('path').resolve(__dirname,'../..')+'/Assets/SkiStation/Gestion/Core/Catalog.Generated.cs',cs);
// --- liste de toutes les clés de statistiques utilisées ---
const keys=new Set();
const funcs=[]; D.EVENTS.forEach(e=>funcs.push(e.need.toString())); D.ACH.forEach(a=>funcs.push(a.test.toString())); D.SCENARIOS.forEach(s=>s.stages.forEach(st=>st.goals.forEach(g=>{funcs.push(g[1].toString()); if(g[2]) funcs.push(g[2].toString());})));
funcs.forEach(f=>{ let m; const re=/\bs\.([a-zA-Z_][\w]*)/g; while((m=re.exec(f))) keys.add(m[1]); });
console.log('LIFTS',D.LIFTS.length,'BUILDINGS',D.BUILDINGS.length,'RESEARCH',D.RESEARCH.length,'WEATHER',W.length,'PROFILES',D.PROFILES.length);
console.log('stat keys:',[...keys].sort().join(' '));
fs.writeFileSync('fn_sources.json',JSON.stringify({events:D.EVENTS.map(e=>({id:e.id,name:e.name,cost:e.cost,boost:e.boost,prestige:e.prestige,fame:e.fame,fameGain:e.fameGain,days:e.days,comp:!!e.comp,bid:!!e.bid,need:e.need.toString(),needTxt:e.needTxt})),ach:D.ACH.map(a=>({id:a.id,name:a.name,desc:a.desc,test:a.test.toString()})),scn:D.SCENARIOS.map(s=>({id:s.id,name:s.name,tag:s.tag,diff:s.diff,seedLock:!!s.seedLock,bid:!!s.bid,brief:s.brief,start:s.start,deadline:s.deadline,victory:s.victory,bankrupt:s.bankrupt,stages:s.stages.map(st=>({title:st.title,reward:st.reward,fame:st.fame,goals:st.goals.map(g=>[g[0],g[1].toString(),g[2]?g[2].toString():null])}))}))},null,1));
