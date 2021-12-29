using System;
using System.Collections.Generic;
using System.IO;

namespace WordFamilies
{
    class Program
    {
        //AI dependant variables
        private static readonly float weightEnd = 150;
        private static readonly float sizeStart = 20;
        private static readonly float sizeEnd = 220;

        //Variables that are set by the game
        private static bool stats;
        private static int wordLength;
        private static int power;
        private static int turns;
        private static List<char> guessed;
        private static char[] target;

        //List for holding all the word families
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

            SetWeight(); //Calculate weights for each letter

            while (true)
            {
                bool difficulty = Menu(); //show player the difficulty selection
                Statistics(); //Asks the player if they want stats

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

                string input = Console.ReadLine().ToLower(); //gets players input
                Console.WriteLine();

                if (input.Length > 1)
                {
                    Console.WriteLine("Please press either 1 or 2");
                }
                else if (input == "1")
                {
                    break; //can break as difSetting is initialized as false
                }
                else if (input == "2")
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
        /// Asks the player if they want word statistics when playing
        /// </summary>
        static void Statistics()
        {
            while (true)
            {
                Console.WriteLine("Would you want to know extra information such as: ");
                Console.WriteLine("How many possible words there are based on letters left");
                Console.WriteLine("and the percentage of words each letter guess narrows the selection by?");
                Console.WriteLine("y / n");
                string input = Console.ReadLine().ToLower(); //gets players input
                Console.WriteLine();

                if (input.Length > 1)
                {
                    Console.WriteLine("Please press either Y or N");
                }
                else if (input == "y")
                {
                    stats = true;
                    break;
                }
                else if (input == "n")
                {
                    stats = false;
                    break;
                }
                else
                {
                    //Tells the player they haven't pressed a valid key
                    Console.WriteLine("Incorrect key pressed!");
                }
            }
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
                string input = Console.ReadLine().ToLower(); //gets players input
                Console.WriteLine();

                if (input.Length > 1)
                {
                    Console.WriteLine("Please press either Y or N");
                }
                else if (input == "y")
                {
                    break; //can break as replay is initialized as true
                }
                else if (input == "n")
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
            wordLength = rng.Next(4, 13); //Creates random number between 4 and 12

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
                //Gets player input then makes word families based on input
                char input = InputPhase();
                Families(input);

                //Finds the word family with the largest amount of words
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

                EvaluteLetter(largestIndex, input); //Sees if inputted letter is in the family

                //Sets wordpool to have the words from the largest word family and resets the word familys
                resetWordPool(largestIndex);
                resetFamilies();

                //Updates the target word
                UpdateTarget(largestIndex, input);
                //Could put in Evaluate letter method as it has a check to see if target contains the letter 
                //Decided here was easier to follow

                //Could have Evaluate() be saved as a variable but don't see the need for it
                if (!EvaluateDashes())
                {
                    break;
                }
            }

            //Evaluate is only called at the once at end of each round or twice for end of game (end round and after round)
            if (EvaluateDashes()) //If - then player lost
            {
                Lost();
            }
            else // no - then player wins
            {
                Winner();
            }
        }

        /// <summary>
        /// Method used for playing on hard
        /// </summary>
        static void Hard()
        {
            while (turns > 0)
            {
                //Gets player input then makes word families based on input
                char input = InputPhase();
                Families(input);

                //Logic for hard mode
                int chosenIndex = 0;
                float largestFuzzy = 0;
                for (int i = 0; i < families.Count; i++)
                {
                    float selectedWeight = PoolWeight(i); //Gets weight for current word pool
                    float fuzzyResult = Fuzzy(selectedWeight, families[i].Count); //Gets a fuzzy value for the current word pool

                    //Finds the word pool with the largest fuzzy result
                    if (fuzzyResult > largestFuzzy)
                    {
                        chosenIndex = i;
                        largestFuzzy = fuzzyResult;
                    }
                    else if (fuzzyResult == largestFuzzy) //if same size make pool with more words as selected pool
                    {
                        if (families[i].Count > families[chosenIndex].Count)
                        {
                            chosenIndex = i;
                            largestFuzzy = fuzzyResult;
                        }
                    }
                }

                EvaluteLetter(chosenIndex, input); //Sees if inputted letter is in the family

                //Resets the word pool to contain the words from the chosen word family then reset the word families
                resetWordPool(chosenIndex);
                resetFamilies();

                //Updates the target word
                UpdateTarget(chosenIndex, input); 
                //Could put in Evaluate letter method as it has a check to see if target contains the letter 
                //Decided here was easier to follow


                //Breaks the loop if no dashes left
                if (!EvaluateDashes())
                {
                    break;
                }
            }

            //Evaluate is only called at the once at end of each round or twice for end of game (end round and after round)
            if (EvaluateDashes()) //If - then player lost
            {
                Lost();
            }
            else // no - then player wins
            {
                Winner();
            }
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

                //Tells the player how many word combinations are possible with the remaining letters if the player said yes to stats
                if(stats)
                {
                    Console.WriteLine("There are a total of: {0} possible word combinations", wordPool.Count);
                }

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
        /// Update the target word with the inputted letter
        /// </summary>
        /// <param name="index">The index is used to set where letters are inserted</param>
        /// <param name="input">The letter to be inserted</param>
        static void UpdateTarget(int index, char input)
        {
            //use index to get binary
            string mask = Convert.ToString(index, 2);

            int offset = wordLength - mask.Length; //used to align the binary instead of inserting leading 0's

            //Loops through mask to find where letters need inserting
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] == '1')
                {
                    target[i + offset] = input; //Sets the target position to inputted letter
                }
            }
        }

        /// <summary>
        /// sees if the inputted character was in the word or not and notifies the player
        /// </summary>
        /// <param name="index">index for word family</param>
        /// <param name="input">user inputted charater</param>
        static void EvaluteLetter(int index, char input)
        {
            //says whether the inputted letter was in the word or not
            if (index == 0)
            {
                turns--;
                Console.WriteLine("{0} was not a letter in the word", input);

                if (stats)//displays reduction percent if player said yes to stats
                {
                    double percent = 100 - Math.Round((families[index].Count / (float)wordPool.Count) * 100);
                    Console.WriteLine("Possible combinations reduced by {0}%", percent);
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("{0} was found in the word!", input);

                if (stats)//displays reduction percent if player said yes to stats
                {
                    double percent = 100 - Math.Round((families[index].Count / (float)wordPool.Count) * 100);
                    Console.WriteLine("Possible combinations reduced by {0}%", percent);
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Sees if there are any - remaining
        /// </summary>
        /// <returns>true for remaining -, false for none</returns>
        static bool EvaluateDashes()
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
        static void SetWeight()
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
        /// Gets the weight for a letter
        /// </summary>
        /// <returns></returns>
        static float GetWeight(char letter)
        {
            float letterWeight;
            
            switch (letter)
            {
                case 'a':
                    letterWeight = weights[0];
                    break;
                case 'b':
                    letterWeight = weights[1];
                    break;
                case 'c':
                    letterWeight = weights[2];
                    break;
                case 'd':
                    letterWeight = weights[3];
                    break;
                case 'e':
                    letterWeight = weights[4];
                    break;
                case 'f':
                    letterWeight = weights[5];
                    break;
                case 'g':
                    letterWeight = weights[6];
                    break;
                case 'h':
                    letterWeight = weights[7];
                    break;
                case 'i':
                    letterWeight = weights[8];
                    break;
                case 'j':
                    letterWeight = weights[9];
                    break;
                case 'k':
                    letterWeight = weights[10];
                    break;
                case 'l':
                    letterWeight = weights[11];
                    break;
                case 'm':
                    letterWeight = weights[12];
                    break;
                case 'n':
                    letterWeight = weights[13];
                    break;
                case 'o':
                    letterWeight = weights[14];
                    break;
                case 'p':
                    letterWeight = weights[15];
                    break;
                case 'q':
                    letterWeight = weights[16];
                    break;
                case 'r':
                    letterWeight = weights[17];
                    break;
                case 's':
                    letterWeight = weights[18];
                    break;
                case 't':
                    letterWeight = weights[19];
                    break;
                case 'u':
                    letterWeight = weights[20];
                    break;
                case 'v':
                    letterWeight = weights[21];
                    break;
                case 'w':
                    letterWeight = weights[22];
                    break;
                case 'x':
                    letterWeight = weights[23];
                    break;
                case 'y':
                    letterWeight = weights[24];
                    break;
                case 'z':
                    letterWeight = weights[25];
                    break;
                default:
                    letterWeight = 0;
                    break; //Shouldn't need a default but just in case
            }

            return letterWeight;
        }

        /// <summary>
        /// Method for calculating the average weight of the word pool.
        /// </summary>
        /// <returns>the average weight of the pool</returns>
        static float PoolWeight(int index)
        {
            float wordTotal;
            float poolAverage = 0;
            
            //Adds each words average letter weight to get total for the pool 
            foreach (string word in families[index])
            {
                //Adds each weight to get the total for a word
                wordTotal = 0;
                foreach (char letter in word)
                {
                    wordTotal += GetWeight(letter);
                }

                poolAverage += wordTotal / word.Length;
            }

            poolAverage = poolAverage / wordPool.Count; //Pool toatal converted to average

            return poolAverage;
        }

        /// <summary>
        /// Applys fuzzy logic to the weight and family size to return a value
        /// </summary>
        /// <param name="weight">average weight for the family</param>
        /// <param name="size">size of the family</param>
        static float Fuzzy(float weight, int size)
        {
            float low = 0;
            float high = 0;
            
            if (size < weightEnd) //Rule 1
            {
                low = 1 - (size / weightEnd);
                //Console.WriteLine("Low result: {0}", low);
            }

            if (size > sizeStart) // Rule 2
            {
                high = (size - sizeStart) / sizeEnd;
                //Console.WriteLine("High result: {0}", high);
            }

            float fuzzyResult = ((low * weight) + (high * size)) / (low + high);
            //Console.WriteLine("Fuzzy: {0}", fuzzyResult);

            return fuzzyResult;
        }



        /// <summary>
        /// Clears screen and displays winner messages
        /// </summary>
        static void Winner()
        {
            //Console.Clear();
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
            //Console.Clear();
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
        /// Sets the wordpool to only have the words in the family given by the index
        /// </summary>
        /// <param name="index">index for a word family</param>
        static void resetWordPool(int index)
        {
            wordPool.Clear();
            foreach (string word in families[index])
            {
                wordPool.Add(word);
            }
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
