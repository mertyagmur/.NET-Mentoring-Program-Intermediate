using PwdGenerator;

PasswordGenerator passwordGenerator = new();

int iterations = 10000;

for (int i = 0; i < iterations; i++)
{
    byte[] salt = passwordGenerator.GenerateSalt();

    string pwd = passwordGenerator.GeneratePasswordHashUsingSalt("password123", salt);
}

for (int i = 0; i < iterations; i++)
{
    byte[] salt = passwordGenerator.GenerateSalt();

    string pwd = passwordGenerator.GeneratePasswordHashUsingSaltOptimized("password123", salt);
}

Console.WriteLine("Done!");
Console.ReadKey();
