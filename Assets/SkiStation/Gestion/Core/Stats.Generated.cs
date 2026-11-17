// FICHIER GÉNÉRÉ (gen_rules.js) : statistiques de la station utilisées par les objectifs, les événements et les succès.
using System.Collections.Generic;
namespace SkiStation.Gestion
{
    public sealed class Stats
    {
        public double lifts, km, easy, red, black, connected, topAlt, beds, bestDay, sat, prestige, access, vert, eventsHosted, worldCup, olympics, fame, debt, machines, patrol, todayV, capacity, minCond, profit7, medals, satAvg, luxBeds, restaurants, activities, stars, money, total, year, loan, research, ticket, parking, bus, food, snack, restaurant, alt_restaurant, gastro, pizzeria, savoyard, drink, bar, rental, shop, lodging, gite, residence, hotel2, chalets, hotel, club, hotel4, palace, luxchalet, refuge, school, kids, pioupiou, rescue, heli, heliport, garage, activity, snowpark, luge, rink, nordic, dogs, paraglide, spa, culture, stage, stadium, olympic, h_fete, h_concert, h_regional, h_europe, h_xgames, h_worldcup, h_olympics;
        public bool bidOk, olyDay1, research_olympic, night, premiumLift;
        /// <summary>Nombre de bâtiments par type ou par identifiant (ex. « food », « palace »).</summary>
        public void AddCount(string key, double n)
        {
            switch (key)
            {
                case "ticket": ticket += n; break;
                case "parking": parking += n; break;
                case "bus": bus += n; break;
                case "food": food += n; break;
                case "snack": snack += n; break;
                case "restaurant": restaurant += n; break;
                case "alt_restaurant": alt_restaurant += n; break;
                case "gastro": gastro += n; break;
                case "pizzeria": pizzeria += n; break;
                case "savoyard": savoyard += n; break;
                case "drink": drink += n; break;
                case "bar": bar += n; break;
                case "rental": rental += n; break;
                case "shop": shop += n; break;
                case "lodging": lodging += n; break;
                case "gite": gite += n; break;
                case "residence": residence += n; break;
                case "hotel2": hotel2 += n; break;
                case "chalets": chalets += n; break;
                case "hotel": hotel += n; break;
                case "club": club += n; break;
                case "hotel4": hotel4 += n; break;
                case "palace": palace += n; break;
                case "luxchalet": luxchalet += n; break;
                case "refuge": refuge += n; break;
                case "school": school += n; break;
                case "kids": kids += n; break;
                case "pioupiou": pioupiou += n; break;
                case "rescue": rescue += n; break;
                case "heli": heli += n; break;
                case "heliport": heliport += n; break;
                case "garage": garage += n; break;
                case "activity": activity += n; break;
                case "snowpark": snowpark += n; break;
                case "luge": luge += n; break;
                case "rink": rink += n; break;
                case "nordic": nordic += n; break;
                case "dogs": dogs += n; break;
                case "paraglide": paraglide += n; break;
                case "spa": spa += n; break;
                case "culture": culture += n; break;
                case "stage": stage += n; break;
                case "stadium": stadium += n; break;
                case "olympic": olympic += n; break;
            }
        }
        public void SetHosted(string id, double n)
        {
            switch (id)
            {
                case "fete": h_fete = n; break;
                case "concert": h_concert = n; break;
                case "regional": h_regional = n; break;
                case "europe": h_europe = n; break;
                case "xgames": h_xgames = n; break;
                case "worldcup": h_worldcup = n; break;
                case "olympics": h_olympics = n; break;
            }
        }
    }
}
