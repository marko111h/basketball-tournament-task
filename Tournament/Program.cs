using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace Tournament
{
    class Program
    {
        static void Main(string[] args)
        {
            // Definišite putanje do fajlova
            string groupsFilePath = @"C:\Users\Marko\source\repos\BasketballTournament\groups.json";
            string exhibitionsFilePath = @"C:\Users\Marko\source\repos\BasketballTournament\exibitions.json";

            try
            {
                var groups = LoadTeamsFromJson(groupsFilePath);

                // Učitaj rezultate prijateljskih utakmica i ažuriraj formu timova
                LoadExhibitionResults(exhibitionsFilePath, groups);

                //// following all match in group faze
                ///
                var groupMatches = new HashSet<(string, string)>();


                var qualifiedTeams = SimulateGroupStage(groups, groupMatches);



                Console.WriteLine("");
                Console.WriteLine("Timovi koji su prošli dalje u nokaut fazu:");
                Console.WriteLine("");
                foreach (var team in qualifiedTeams)
                {
                    Console.WriteLine($"{team.Name} - Rank: {team.FibaRank}, Points: {team.Points}, Wins/Losses: {team.Wins}/{team.Losses}, For/Against: {team.ForPoints}/{team.AgainstPoints},Point Difference: {team.PointDifference} ");
                }
                Console.WriteLine("");
                // Prikaz timova koji su prošli dalje u nokaut fazu, sortirano od najboljeg ka najgorem
                Console.WriteLine("Timovi koji su prošli dalje u nokaut fazu (sortirano po bodovima):");
                var sortedQualifiedTeams = qualifiedTeams.OrderByDescending(t => t.Points)
                                                         .ThenByDescending(t => t.PointDifference)
                                                         .ThenByDescending(t => t.ForPoints)
                                                         .ToList();

                foreach (var team in sortedQualifiedTeams)
                {
                    Console.WriteLine($"{team.Name} - Rank: {team.FibaRank}, Points: {team.Points}, Wins/Losses: {team.Wins}/{team.Losses}, For/Against: {team.ForPoints}/{team.AgainstPoints}, Point Difference: {team.PointDifference}");
                }

                // Pronalaženje najgoreg tima (poslednji u sortiranom nizu)
                var worstTeam = sortedQualifiedTeams.Last();
                Console.WriteLine($"\nNajgori tim je: {worstTeam.Name} - Rank: {worstTeam.FibaRank}, Points: {worstTeam.Points}, Wins/Losses: {worstTeam.Wins}/{worstTeam.Losses}, For/Against: {worstTeam.ForPoints}/{worstTeam.AgainstPoints},Point Difference: {worstTeam.PointDifference}");
                Console.WriteLine("");
                qualifiedTeams.Remove(worstTeam);
                // Izvrši simulaciju eliminacione faze
                SimulateKnockoutStage(qualifiedTeams, groupMatches);

                Console.WriteLine("Kraj Turnira");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Greška: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Došlo je do greške: {ex.Message}");
            }




            static Dictionary<string, List<Team>> LoadTeamsFromJson(string filePath)
            {
                var json = File.ReadAllText(filePath);
                var groups = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(json);

                var result = new Dictionary<string, List<Team>>();

                foreach (var group in groups)
                {
                    var groupList = new List<Team>();
                    foreach (var teamData in group.Value)
                    {

                        var team = new Team
                        {
                            Name = teamData["Team"].ToString(),
                            FibaRank = Convert.ToInt32(teamData["FIBARanking"])
                        };
                        groupList.Add(team);
                    }
                    result[group.Key] = groupList;
                }

                return result;
            }


            /// funcion for simul match
            /// 
            static (int, int) SimulateMatch(Team teamA, Team teamB)
            {
                // Izračunavanje prednosti
                double advantage = (teamB.FibaRank - teamA.FibaRank) * 2 + (teamA.Form - teamB.Form) * 0.5; // Faktor 2 za prilagođavanje

                // Generisanje poena
                Random random = new Random();
                int baseScore = 90; // Osnovni broj poena
                int scoreA = (int)(baseScore + random.NextDouble() * 20 + advantage);
                int scoreB = (int)(baseScore + random.NextDouble() * 20 - advantage);

                // Osiguravanje da poeni budu u opsegu od 60 do 120
                scoreA = Math.Clamp(scoreA, 60, 120);
                scoreB = Math.Clamp(scoreB, 60, 120);

                // Ažuriraj formu nakon meča
                UpdateForm(scoreA > scoreB ? teamA : teamB, scoreA > scoreB ? teamB : teamA, Math.Abs(scoreA - scoreB));


                return (scoreA, scoreB);
            }



            static List<Team> SimulateGroupStage(Dictionary<string, List<Team>> groups, HashSet<(string, string)> groupMatches)
            {
                var roundResults = new Dictionary<int, List<(Team, Team, int, int)>>(); // Ključ je kolo, vrednost je lista mečeva sa rezultatima
                var qualifiedTeams = new List<Team>();



                foreach (var group in groups)
                {
                    Console.WriteLine("");
                    Console.WriteLine($"Grupa {group.Key}:");
                    var teams = group.Value;

                    /// inicijlizuj recnik za rezultate po kolima
                    /// 
                    var firstRound = new Dictionary<string, string>();
                    var secondRound = new Dictionary<string, string>();
                    var thirdRound = new Dictionary<string, string>();


                    // Simulirajte sve utakmice u grupi
                    for (int i = 0; i < teams.Count; i++)
                    {
                        for (int j = i + 1; j < teams.Count; j++)
                        {
                            var teamA = teams[i];
                            var teamB = teams[j];
                            var (scoreA, scoreB) = SimulateMatch(teamA, teamB);

                            // Beleženje odigranih mečeva
                            groupMatches.Add((teamA.Name, teamB.Name));


                            // Ažurirajte statistiku
                            if (scoreA > scoreB)
                            {
                                teamA.Wins++;
                                teamB.Losses++;
                                teamA.Points += 2;
                            }
                            else if (scoreB > scoreA)
                            {
                                teamB.Wins++;
                                teamA.Losses++;
                                teamB.Points += 2;
                            }
                            else
                            {
                                teamA.Points++;
                                teamB.Points++;
                            }

                            teamA.ForPoints += scoreA;
                            teamA.AgainstPoints += scoreB;
                            teamB.ForPoints += scoreB;
                            teamB.AgainstPoints += scoreA;

                            // Dodavanje meča u odgovarajuće kolo
                            if (i == 0 && j == 1 || i == 2 && j == 3)
                                firstRound.Add($"{teamA.Name} - {teamB.Name}", $"{scoreA}:{scoreB}");
                            else if ((i == 0 && j == 2) || (i == 1 && j == 3))
                                secondRound.Add($"{teamA.Name} - {teamB.Name}", $"{scoreA}:{scoreB}");
                            else
                                thirdRound.Add($"{teamA.Name} - {teamB.Name}", $"{scoreA}:{scoreB}");
                        }
                     
                    }

                    // Prikaz rezultata po kolima
                    Console.WriteLine("\nRezultati po kolima:");
                    Console.WriteLine(" Kolo 1:");
                    foreach (var match in firstRound)
                        Console.WriteLine($"{match.Key} ({match.Value})");

                    Console.WriteLine(" Kolo 2:");
                    foreach (var match in secondRound)
                        Console.WriteLine($"{match.Key} ({match.Value})");

                    Console.WriteLine(" Kolo 3:");
                    foreach (var match in thirdRound)
                        Console.WriteLine($"{match.Key} ({match.Value})");

                    // Sortirajte timove po bodovima, zatim koš razlici, i na kraju postignutim koševima
                    var sortedTeams = teams.OrderByDescending(t => t.Points)
                                           .ThenByDescending(t => t.PointDifference)
                                           .ThenByDescending(t => t.ForPoints)
                                           .ToList();

                    // Prikaz rangiranja u grupi
                    Console.WriteLine("");
                    Console.WriteLine("Konačan plasman u grupi:");
                    for (int i = 0; i < sortedTeams.Count; i++)
                    {
                        var team = sortedTeams[i];
                        Console.WriteLine($"{i + 1}. {team.Name} {team.Wins}/{team.Losses}/{team.Points}/{team.ForPoints}/{team.AgainstPoints}/{team.PointDifference}");
                    }

                    // prva 3 tima iz svake grupa idu dalje
                    qualifiedTeams.AddRange(sortedTeams.Take(3));
                }
                return qualifiedTeams;
            }


            static void SimulateKnockoutStage(List<Team> teams, HashSet<(string, string)> groupMatches)
            {
                var sortedTeams = teams.OrderByDescending(t => t.Points)
                          .ThenByDescending(t => t.PointDifference)
                          .ThenByDescending(t => t.ForPoints)
                          .ToList();

                var hatD = sortedTeams.Take(2).ToList();
                var hatE = sortedTeams.Skip(2).Take(2).ToList();
                var hatF = sortedTeams.Skip(4).Take(2).ToList();
                var hatG = sortedTeams.Skip(6).Take(2).ToList();

                // Prikaz šešira
                Console.WriteLine("Šeširi:");
                PrintHat("D", hatD);
                PrintHat("E", hatE);
                PrintHat("F", hatF);
                PrintHat("G", hatG);

                //// random 
                ///
                // Ensure that the matchups do not include teams that faced each other in the group stage
                var matchups = new List<(Team, Team)>();

                // Ukštanje šešira D sa G
                matchups.AddRange(GenerateMatchups(hatD, hatE, hatF, hatG, groupMatches));

                // Ukštanje šešira E sa F
                //  matchups.AddRange(GenerateMatchups(hatE, hatF, groupMatches));

                // Prikaz eliminacione faze
                Console.WriteLine("");
                Console.WriteLine("Eliminaciona faza:");
                Console.WriteLine("");
                var quarterFinalWinners = new List<Team>();
                foreach (var (teamA, teamB) in matchups)
                {
                    var (scoreA, scoreB) = SimulateMatch(teamA, teamB);
                    Console.WriteLine($"{teamA.Name} - {teamB.Name} ({scoreA}:{scoreB})");
                    if (scoreA > scoreB)
                    {
                        quarterFinalWinners.Add(teamA);
                    }
                    else
                    {
                        quarterFinalWinners.Add(teamB);
                    }
                }
                var semiFinalWinners = new List<Team>();
                var finalWinners = new List<Team>();
                var bronzeMatchTeams = new List<Team>();

                Console.WriteLine("\nPolufinale:");
                for (int i = 0; i < 2; i++)
                {
                    var (scoreA, scoreB) = SimulateMatch(quarterFinalWinners[i], quarterFinalWinners[i + 2]);
                    var winner = scoreA > scoreB ? quarterFinalWinners[i] : quarterFinalWinners[i + 2];
                    var loser = scoreA > scoreB ? quarterFinalWinners[i + 2] : quarterFinalWinners[i];

                    bronzeMatchTeams.Add(loser);
                    semiFinalWinners.Add(winner);
                    finalWinners.Add(winner);

                    Console.WriteLine($"{quarterFinalWinners[i].Name} ({scoreA}) - {quarterFinalWinners[i + 2].Name} ({scoreB}) -> {winner.Name} ide dalje.");
                }
                Console.WriteLine("\nFinale:");
                var finalScore = SimulateMatch(finalWinners[0], finalWinners[1]);
                var champion = finalScore.Item1 > finalScore.Item2 ? finalWinners[0] : finalWinners[1];
                Console.WriteLine($"{finalWinners[0].Name} ({finalScore.Item1}) - {finalWinners[1].Name} ({finalScore.Item2}) -> {champion.Name} je pobednik!");

                Console.WriteLine("\nBronzani meč:");
                var bronzeMatchScore = SimulateMatch(bronzeMatchTeams[0], bronzeMatchTeams[1]);
                var bronzeWinner = bronzeMatchScore.Item1 > bronzeMatchScore.Item2 ? bronzeMatchTeams[0] : bronzeMatchTeams[1];
                Console.WriteLine($"{bronzeMatchTeams[0].Name} ({bronzeMatchScore.Item1}) - {quarterFinalWinners[1].Name} ({bronzeMatchScore.Item2}) -> {bronzeWinner.Name} osvaja bronzanu medalju!");

                static void PrintHat(string hatName, List<Team> teams)
                {
                    Console.WriteLine($"    Šešir {hatName}");
                    foreach (var team in teams)
                    {
                         Console.WriteLine($"\t{team.Name}");
                    }
                }

                static List<(Team, Team)> GenerateMatchups(List<Team> hatD, List<Team> hatE, List<Team> hatF, List<Team> hatG, HashSet<(string, string)> groupMatches)
                {
                    var random = new Random();
                    var matchups = new List<(Team, Team)>();

                    // Kombinovanje šešira za lakše pretragu
                    var hatDE = hatD.Concat(hatE).ToList();
                    var hatFG = hatF.Concat(hatG).ToList();

                    while (hatDE.Any() && hatFG.Any())
                    {
                        var teamA = hatDE[random.Next(hatDE.Count)];
                        //   Team teamB;

                        // Filtriraj timove koji nisu igrali protiv tima A
                        var validOpponents = hatFG.Where(teamB =>
                            !groupMatches.Contains((teamA.Name, teamB.Name)) &&
                            !groupMatches.Contains((teamB.Name, teamA.Name))
                        ).ToList();

                        // Ako nema validnih protivnika, preskoči ovaj tim
                        if (!validOpponents.Any())
                        {
                            Console.WriteLine($"Nema validnih protivnika za {teamA.Name}. Uklanjam iz hatDE.");
                            hatDE.Remove(teamA);

                            if (!hatDE.Any())
                            {
                                Console.WriteLine("Nema više timova u hatDE. Izlazak.");
                                break;
                            }
                            continue;
                        }

                        // Odaberi nasumično protivnika iz validnih
                        var teamB = validOpponents[random.Next(validOpponents.Count)];


                        // Dodaj par u listu matchupa i ukloni timove iz šešira
                        matchups.Add((teamA, teamB));
                        hatDE.Remove(teamA);
                        hatFG.Remove(teamB);
                    }
                  
                    return matchups;
                }
             




            }
            static void LoadExhibitionResults(string filePath, Dictionary<string, List<Team>> groups)
            {
                var json = File.ReadAllText(filePath);
                var exhibitions = JsonConvert.DeserializeObject<Dictionary<string, List<Match>>>(json);

                foreach (var exhibition in exhibitions)
                {
                    var group = groups.Values.SelectMany(g => g).ToList();
                    var team = group.FirstOrDefault(t => t.Name == exhibition.Key);

                    if (team != null)
                    {
                        foreach (var result in exhibition.Value)
                        {
                            // Parsiranje rezultata da se dobiju poeni
                            var scores = result.Result.Split('-');
                            if(scores.Length == 2)
                            {
                                int teamPoints = int.Parse(scores[0]);
                                int opponentPoints = int.Parse(scores[1]);

                                var opponent = group.FirstOrDefault(t => t.Name == result.Opponent);
                                if (opponent != null)
                                {
                                    // Dodaj poene za formu na osnovu razlike u poenima i FIBA ranga protivnika
                                    team.Form += (teamPoints - opponent.ForPoints) * (double)opponent.FibaRank / 100;

                                }

                            }
                            
                        }
                    }
                }
            }

            // Ažuriranje forme nakon svake utakmice
            static void UpdateForm(Team winner, Team loser, int scoreDifference)
            {
                // Pobednik dobija poene za formu na osnovu razlike u poenima
                winner.Form += scoreDifference;

                // Gubitnik gubi poene za formu na osnovu razlike u poenima
                loser.Form -= scoreDifference;
            }


        }
    }

}