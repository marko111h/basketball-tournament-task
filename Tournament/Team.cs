using System;

public class Team
{
    public required string Name { get; set; }
    public int FibaRank { get; set; }
    public int Points { get; set; } = 0;
    public int Wins { get; set; } = 0;
    public int Losses { get; set; } = 0;
    public int ForPoints { get; set; } = 0; // Points scored
    public int AgainstPoints { get; set; } = 0; // Points conceded
    public int PointDifference => ForPoints - AgainstPoints;
}
