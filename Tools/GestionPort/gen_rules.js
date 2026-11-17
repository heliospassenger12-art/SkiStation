global.window=global; global.THREE=require('three');
for (const f of ['core','terrain','data']) require(require('path').resolve(__dirname,'../..')+'/Web/js/'+f+'.js');
const D=SS.Data, fs=require('fs'), src=JSON.parse(fs.readFileSync(require('path').join(__dirname,'fn_sources.json')));
const q=s=>'"'+String(s).replace(/\\/g,'\\\\').replace(/"/g,'\\"')+'"';
const BOOL=['bidOk','olyDay1','research_olympic','night','premiumLift'];
function conv(js){
  let c=js;
  c=c.replace(/\(s\.(\w+) \|\| 0\)/g,'s.$1');
  c=c.replace(/Math\.abs\(/g,'Math.Abs(').replace(/\[0, 1, 2, 3\]\.every\(d => G\.pistes\.some\(p => (.*?)\)\)/g,'new[] { 0, 1, 2, 3 }.All(d => G.pistes.Any(p => $1))');
  c=c.replace(/SS\.num\(/g,'GMath.Num(').replace(/Math\.round\(/g,'Math.Round(').replace(/Math\.min\(/g,'Math.Min(').replace(/Math\.max\(/g,'Math.Max(');
  c=c.replace(/\.toFixed\((\d)\)/g,(m,d)=>'.ToString("'+(d==='0'?'0':'0.'+'0'.repeat(+d))+'")');
  c=c.replace(/SS\.Scn\.critOk\(/g,'Scenarios.CritOk(').replace(/SS\.Scn\.critShare\(/g,'Scenarios.CritShare(').replace(/SS\.researched\(/g,'Game.Researched(');
  c=c.replace(/v\('(\w+)'\)/g,'s => GMath.Num(s.$1)');
  c=c.replace(/===/g,'==').replace(/!==/g,'!=');
  c=c.replace(/'((?:[^'\\]|\\.)*)'/g,(m,a)=>q(a.replace(/\\'/g,"'")));
  // Math.Round renvoie un double : son affichage doit être un entier
  c=c.replace(/Math\.Round\(([^()]*(?:\([^()]*\))?[^()]*)\) \+ "/g,'Math.Round($1).ToString("0") + "');
  return c;
}
let out=`// FICHIER GÉNÉRÉ depuis Web/js/data.js (script gen_rules.js) : événements, succès et scénarios.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkiStation.Gestion
{
    public sealed class EventDef { public string id, name, needTxt; public double cost, boost, prestige, fame, fameGain; public int days; public bool comp, bid; public Func<Stats, bool> need; }
    public sealed class AchDef { public string id, name, desc; public Func<Stats, GameState, bool> test; }
    public sealed class Goal { public string text; public Func<Stats, bool> test; public Func<Stats, string> val; }
    public sealed class StageDef { public string title; public double reward, fame; public Goal[] goals; }
    public sealed class ScenarioStart { public double money, fame = 5, rep = -1, sat = -1; public int seed; public string prebuilt; public string[] research = new string[0]; }
    public sealed class ScenarioDef { public string id, name, tag, brief, victory; public int diff, deadline; public bool seedLock, bid; public double bankrupt; public ScenarioStart start; public StageDef[] stages; }

    public static partial class Catalog
    {
        public static readonly EventDef[] EVENTS = {
${src.events.map(e=>`            new EventDef { id = ${q(e.id)}, name = ${q(e.name)}, cost = ${e.cost}, boost = ${e.boost}, prestige = ${e.prestige}, fame = ${e.fame}, fameGain = ${e.fameGain}, days = ${e.days}, comp = ${e.comp}, bid = ${e.bid}, needTxt = ${q(e.needTxt)}, need = ${conv(e.need)} }`).join(',\n')}
        };
        public static readonly AchDef[] ACH = {
${src.ach.map(a=>{ let t=a.test; if(!/\(s, G\)/.test(t)) t=t.replace(/^s =>/,'(s, G) =>'); return `            new AchDef { id = ${q(a.id)}, name = ${q(a.name)}, desc = ${q(a.desc)}, test = ${conv(t)} }`; }).join(',\n')}
        };
        public static readonly ScenarioDef[] SCENARIOS = {
${src.scn.map(s=>`            new ScenarioDef { id = ${q(s.id)}, name = ${q(s.name)}, tag = ${q(s.tag)}, diff = ${s.diff}, seedLock = ${s.seedLock}, bid = ${s.bid}, deadline = ${s.deadline||0}, bankrupt = ${s.bankrupt||200000}, brief = ${q(s.brief)}, victory = ${q(s.victory)},
                start = new ScenarioStart { money = ${s.start.money}, seed = ${s.start.seed}, fame = ${s.start.fame||5}${s.start.rep!==undefined?', rep = '+s.start.rep:''}${s.start.sat!==undefined?', sat = '+s.start.sat:''}${s.start.prebuilt?', prebuilt = '+q(s.start.prebuilt):''}, research = new string[] { ${(s.start.research||[]).map(q).join(', ')} } },
                stages = new[] {
${s.stages.map(st=>`                    new StageDef { title = ${q(st.title)}, reward = ${st.reward||0}, fame = ${st.fame||0}, goals = new[] {
${st.goals.map(g=>`                        new Goal { text = ${q(g[0])}, test = ${conv(g[1])}${g[2]?', val = '+(/s\[k\]/.test(g[2])?'s => GMath.Num(s.'+g[1].match(/s\.(\w+)/)[1]+')':conv(g[2])):''} }`).join(',\n')} } }`).join(',\n')} } }`).join(',\n')}
        };
    }
}
`;
fs.writeFileSync(require('path').resolve(__dirname,'../..')+'/Assets/SkiStation/Gestion/Core/Catalog.Rules.Generated.cs',out);
// classe Stats : toutes les clés utilisées + comptes de bâtiments
const base=['lifts','km','easy','red','black','connected','topAlt','beds','bestDay','sat','prestige','access','vert','eventsHosted','worldCup','olympics','fame','debt','machines','patrol','todayV','capacity','minCond','profit7','medals','satAvg','luxBeds','restaurants','activities','stars','money','total','year','loan','research'];
const counts=new Set(); D.BUILDINGS.forEach(b=>{counts.add(b.type); counts.add(b.id);});
const hk=D.EVENTS.map(e=>'h_'+e.id);
const all=[...new Set(base.concat([...counts],hk))].filter(k=>!BOOL.includes(k));
let st=`// FICHIER GÉNÉRÉ (gen_rules.js) : statistiques de la station utilisées par les objectifs, les événements et les succès.
using System.Collections.Generic;
namespace SkiStation.Gestion
{
    public sealed class Stats
    {
        public double ${all.join(', ')};
        public bool ${BOOL.join(', ')};
        /// <summary>Nombre de bâtiments par type ou par identifiant (ex. « food », « palace »).</summary>
        public void AddCount(string key, double n)
        {
            switch (key)
            {
${[...counts].map(k=>`                case ${q(k)}: ${k} += n; break;`).join('\n')}
            }
        }
        public void SetHosted(string id, double n)
        {
            switch (id)
            {
${D.EVENTS.map(e=>`                case ${q(e.id)}: h_${e.id} = n; break;`).join('\n')}
            }
        }
    }
}
`;
fs.writeFileSync(require('path').resolve(__dirname,'../..')+'/Assets/SkiStation/Gestion/Core/Stats.Generated.cs',st);
console.log('ok', all.length, 'champs');
