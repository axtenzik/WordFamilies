using System;
using System.Collections.Generic;
using System.IO;

namespace WordFamilies
{
    class Program
    {
        //Variables that are set by the game
        private static int wordLength;
        private static int turns;
        private static int power;
        private static List<char> guessed;
        private static char[] target;

        private static List<List<string>> families;

        //Sets the dictionary and the letters that can be guessed
        private static string[] dictionary;
        private static List<string> wordPool;
        private static readonly char[] alphabet = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'};
        private static float[] weights = new float[26];
        private static List<char> remainingLetters;
        
        static void Main(string[] args)
        {
            //Try catch in case dictionary.txt is not found or unable to be read
            try
            {
                dictionary = File.ReadAllLines("dictionary.txt"); //insert words into dictionary string array
            }
            catch
            {
                Console.WriteLine("Unable to read or find dictionary.txt");
                Console.WriteLine("Closing Program");
                Console.ReadKey();
                Environment.Exit(2);
                //Microsoft uses exit code 2 for missing file so thought I should too
                //https://docs.microsoft.com/en-gb/windows/win32/debug/system-error-codes--0-499-?redirectedfrom=MSDN
            }

            WeightSetUp(); //Calculate weights for each letter

            while (true)
            {
                bool difficulty = Menu(); //show player the difficulty selection

                Initialize(); //initialize the variables

                if (!difficulty) //if false play easy
                {
                    Easy();
                }
                else //if true play hard
                {
                    Hard();
                }

                if(!Replay())
                {
                    break;
                }
            }
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(); //Stops program from instantly closing
        }



        /// <summary>
        /// Start menu to select difficulty
        /// </summary>
        /// <returns>false for easy, true for hard</returns>
        static bool Menu()
        {
            bool difSetting = false;

            while (true) //loops in case player doesn't select right key
            {
                //asks the player what difficulty they want to play
                Console.WriteLine("Pleae select a difficulty level by entering 1 or 2:");
                Console.WriteLine();
                Console.WriteLine("1. Easy");
                Console.WriteLine("2. Hard");

                char input = Console.ReadKey().KeyChar; //gets players input
                Console.WriteLine("\n"); // \n for some added spacing

                if (input == '1')
                {
                    break; //can break as difSetting is initialized as false
                }
                else if (input == '2')
                {
                    difSetting = true; //hard is true so change difSetting to true
                    break; //break to get out loop
                }
                else
                {
                    //Tells the player they haven't pressed a valid key
                    Console.WriteLine("Incorrect key pressed!");
                }
            }

            return difSetting;
        }

        /// <summary>
        /// Menu for asking the player if they want to replay
        /// </summary>
        /// <returns>false for no, true for yes</returns>
        static bool Replay()
        {
            bool replay = true;
            
            while (true)
            {
                Console.WriteLine("Do you want to play again? y/n");
                char input = Console.ReadKey().KeyChar; //gets players input
                Console.WriteLine("\n"); // \n for some added spacing

                if (input == 'y')
                {
                    break; //can break as replay is initialized as true
                }
                else if (input == 'n')
                {
                    replay = false; //sets replay to false
                    break; //break to get out loop
                }
                else
                {
                    //Tells the player they haven't pressed a valid key
                    Console.WriteLine("Incorrect key pressed!");
                }
            }

            return replay;
        }



        /// <summary>
        /// Initializes variables
        /// </summary>
        static void Initialize()
        {
            //Chooses length of word to be guessed
            Random rng = new Random();
            wordLength = rng.Next(4, 12);

            //sets turns and the lists
            turns = wordLength * 2;
            wordPool = new List<string>();
            guessed = new List<char>();
            remainingLetters = new List<char>(alphabet);

            //initializes the Lists that the word families will be saved to
            //up to 2^12 different families the words can be sorted to, sets up based on wordLength
            power = (int)Math.Pow(2, wordLength);
            families = new List<List<string>>();
            resetFamilies();

            //sets the guessable word and sets the slots for each letter to be slots to be -
            target = new char[wordLength];
            for (int i = 0; i < wordLength; i++)
            {
                target[i] = '-';
            }

            //Adds words of length wordLength to word pool
            foreach (string word in dictionary)
            {
                if (word.Length == wordLength)
                {
                    wordPool.Add(word);
                }
            }
        }


        /// <summary>
        /// Method used for playing on easy
        /// </summary>
        static void Easy()
        {
            while (turns > 0) //Play loop
            {
                char input = InputPhase();
                Families(input);

                int largestIndex = 0;
                int largestCount = 0;
                for (int i = 0; i < families.Count; i++)
                {
                    if (families[i].Count > largestCount)
                    {
                        largestIndex = i;
                        largestCount = families[i].Count;
                    }
                }

                if (largestIndex == 0)
                {
                    turns--;
                    Console.WriteLine("{0} was not a letter in the word", input);
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("{0} was found in the word!", input);
                    Console.WriteLine();
                }

                wordPool.Clear();
                foreach (string word in families[largestIndex])
                {
                    wordPool.Add(word);
                }
                resetFamilies();

                //use index to get binary
                string mask = Convert.ToString(largestIndex, 2);


                int offset = wordLength - mask.Length; //used to align the binary
                for (int i = 0; i < mask.Length; i++)
                {
                    if (mask[i] == '1')
                    {
                        target[i + offset] = input;
                    }
                }

                if (!Evaluate())
                {
                    break;
                }
            }

            if (Evaluate())
            {
                Lost();
            }
            else
            {
                Winner();
            }
        }

        static void Hard()
        {
            foreach (int numer in weights)
            {
                Console.WriteLine(numer);
            }
            Console.ReadKey();
            
            /*while (turns > 0)
            {
                char input = InputPhase();

                for (int i = 0; i < families.Count; i++)
                {

                }
            }//*/
        }



        /// <summary>
        /// player input phase when playing the game. Gets the players input, checks its valid then returns the letter
        /// </summary>
        /// <returns>letter pressed by the player</returns>
        static char InputPhase()
        {
            //Variable for storing the player input
            //string instead of char as char doesn't have a to lower method (or I couldn't find one)
            string input;

            //Loop so if the player does not enter a valid letter, the program can ask them again
            while (true)
            {
                Console.WriteLine("You have {0} guesses remaining", turns); //Display guesses remaining

                //Lists the currently correctly guessed letters
                Console.Write("Current correctly guessed letters: ");
                for (int i = 0; i < target.Length; i++)
                {
                    Console.Write("{0} ", target[i]);
                }
                Console.WriteLine();

                //Lists the previously guessed letters
                if (guessed.Count > 0)
                {
                    Console.Write("Previously guessed letters: ");
                    for (int i = 0; i < guessed.Count; i++)
                    {
                        Console.Write("{0} ", guessed[i]);
                    }
                    Console.WriteLine();
                }

                //Lists all the letters the player can still use to guess
                Console.Write("Remaining letters: ");
                for (int i = 0; i < remainingLetters.Count; i++)
                {
                    Console.Write("{0} ", remainingLetters[i]);
                }
                Console.WriteLine();

                //asks the player to enter a letter and stores it
                Console.Write("Please guess a letter then hit enter: ");
                input = Console.ReadLine().ToLower();

                Console.WriteLine();
                
                if (input.Length > 1) //doesn't accept inputs of more than one letter
                {
                    Console.WriteLine("Too many letters inputted!");
                    Console.WriteLine();
                }
                else if (input.Length < 1) //Doesn't accept no input
                {
                    Console.WriteLine("Please enter a letter!");
                    Console.WriteLine();
                }
                else
                {
                    //variables for previously used letter and legal letter
                    bool previous = false;
                    bool valid = false;

                    //Checks to see if the inputted character is a letter
                    for (int i = 0; i < alphabet.Length; i++)
                    {
                        if (input[0] == alphabet[i])
                        {
                            valid = true;
                        }
                    }
                    
                    //Checks to see if the letter has been previously guessed
                    for (int i = 0; i < guessed.Count; i++)
                    {
                        if (input[0] == guessed[i])
                        {
                            previous = true;
                        }
                    }

                    if (!previous && valid) //Letter not previously guessed and is a valid letter
                    {
                        guessed.Add(input[0]); //add current inputted letter to previous guess pool
                        remainingLetters.Remove(input[0]); //removes the guessed letter from the remaining letters pool
                        break; //breaks the loop so player is not asked to enter another letter on same turn
                    }
                    else
                    {
                        Console.WriteLine("Please guess a different letter!"); //Letter has either been guessed before or is not a letter
                        Console.WriteLine();
                    }
                }
            }
            return input[0];
        }



        /// <summary>
        /// Sees if there are any - remaining
        /// </summary>
        /// <returns>true for remaining -, false for none</returns>
        static bool Evaluate()
        {
            bool dashes = false;

            //loops through to find -
            for (int i = 0; i < target.Length; i++)
            {
                if (target[i] == '-')
                {
                    dashes = true;
                }
            }

            return dashes;
        }


        
        /// <summary>
        /// Sets up the word weights used in hard algorithm game
        /// </summary>
        static void WeightSetUp()
        {
            int[] frequency = new int[26];

            //cycles through each letter in each word
            foreach (string word in dictionary)
            {
                foreach (char letter in word)
                {
                    //each letter appearance in dictionary is used automatically to set up the weight
                    switch (letter)
                    {
                        case 'a':
                            frequency[0]++;
                            break;
                        case 'b':
                            frequency[1]++;
                            break;
                        case 'c':
                            frequency[2]++;
                            break;
                        case 'd':
                            frequency[3]++;
                            break;
                        case 'e':
                            frequency[4]++;
                            break;
                        case 'f':
                            frequency[5]++;
                            break;
                        case 'g':
                            frequency[6]++;
                            break;
                        case 'h':
                            frequency[7]++;
                            break;
                        case 'i':
                            frequency[8]++;
                            break;
                        case 'j':
                            frequency[9]++;
                            break;
                        case 'k':
                            frequency[10]++;
                            break;
                        case 'l':
                            frequency[11]++;
                            break;
                        case 'm':
                            frequency[12]++;
                            break;
                        case 'n':
                            frequency[13]++;
                            break;
                        case 'o':
                            frequency[14]++;
                            break;
                        case 'p':
                            frequency[15]++;
                            break;
                        case 'q':
                            frequency[16]++;
                            break;
                        case 'r':
                            frequency[17]++;
                            break;
                        case 's':
                            frequency[18]++;
                            break;
                        case 't':
                            frequency[19]++;
                            break;
                        case 'u':
                            frequency[20]++;
                            break;
                        case 'v':
                            frequency[21]++;
                            break;
                        case 'w':
                            frequency[22]++;
                            break;
                        case 'x':
                            frequency[23]++;
                            break;
                        case 'y':
                            frequency[24]++;
                            break;
                        case 'z':
                            frequency[25]++;
                            break;
                        default:
                            break; //Shouldn't need a default but just in case
                    }
                }
            }

            //gets max value in frequency
            int maxValue = 0;
            for (int i = 0; i < frequency.Length; i++)
            {
                if (frequency[i] > maxValue)
                {
                    maxValue = frequency[i];
                }
            }
            
            //Loop to get weight of each letter as percentage
            //finds out the percentage that is NOT the letter
            //This is so the least common letters have the highest weights
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = (float)(maxValue - frequency[i]) / (float)(maxValue) * 100f;
            }
        }



        /// <summary>
        /// Clears screen and displays winner messages
        /// </summary>
        static void Winner()
        {
            Console.Clear();
            Console.WriteLine("Congratulations!");
            Console.WriteLine("You are a winner!");
            Console.WriteLine();

            //Displays the word that was guessed
            Console.Write("The word was: ");
            for (int i = 0; i < target.Length; i++)
            {
                Console.Write(target[i]);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Clears screen and displays loser messages
        /// </summary>
        static void Lost()
        {
            Console.Clear();
            Console.WriteLine("Out of turns");
            Console.WriteLine("Better luck next time!");
            Console.WriteLine();
            Console.Write("The word was: ");

            //Randomly gets a word from the word pool and shows the player it as the failed to guess word
            Random rng = new Random();
            int cpuWord = rng.Next(0, wordPool.Count - 1);
            Console.WriteLine(wordPool[cpuWord]);
            Console.WriteLine();
        }



        /// <summary>
        /// Clears the word families so on next turn they can be calculated again
        /// </summary>
        static void resetFamilies()
        {
            families.Clear();
            for (int i = 0; i < power; i++)
            {
                families.Add(new List<string>());
            }
        }

        /// <summary>
        /// Inserts all the words in word pool into word families
        /// </summary>
        /// <param name="input">Player inputted letter</param>
        static void Families(char input)
        {
            //Loops through the words in word pool
            foreach (string word in wordPool)
            {
                int index = Family(word, input); //gets family index based on word and input
                families[index].Add(word); //inserts word into its word family
            }
        }

        /// <summary>
        /// Gets the family the current word belongs to based on inputted letter
        /// </summary>
        /// <param name="word">current word from word pool</param>
        /// <param name="input">player inputted letter</param>
        /// <returns>The index for family</returns>
        static int Family(string word, char input)
        {
            char[] binary = new char[wordLength]; //Sets up array the binary numbers will be inserted into

            //Loops through the word
            for (int i = 0; i < wordLength; i++)
            {
                if (word[i] == input) //if letter in word is same as input then make 1
                {
                    binary[i] = '1';
                }
                else //if not make 0
                {
                    binary[i] = '0';
                }
            }

            //Put binary char array into a string
            string binaryString = new string(binary);

            //Convert the binary to int
            int familyNo = Convert.ToInt32(binaryString, 2); //int is the family number the word belongs to

            return familyNo;
        }
    }
}
