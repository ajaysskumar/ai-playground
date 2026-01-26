public class ArgumentParser
{
    private readonly string[] _args;

    public ArgumentParser(string[] args)
    {
        _args = args;
    }

    public string GetDemo()
    {
        for (int i = 0; i < _args.Length; i++)
        {
            if (_args[i] == "--demo" && i + 1 < _args.Length)
            {
                return _args[i + 1];
            }
        }

        return "bedrock-movie";
    }
}