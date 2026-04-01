try
{
    using var game = new Alea_Jacta_Est.Game1();
    game.Run();
}
catch (Exception ex)
{
    var crashPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.txt");
    System.IO.File.WriteAllText(crashPath, ex.ToString());
    throw;
}
