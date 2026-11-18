RUN INSTRUCTIONS

- Go to the file in Assets/TextFiles/OpenRouterKey
- Open the file and paste your key
- To change the model (i.e. to one that doesn't have the limitations that have been plaguing me), go to the GameController GameObject in the Main scene, look at the AI Chat Controller component, and change the Model value to your model of choice.

MODEL OUTPUTS

The model has various different outputs, either json files (most common) or straight text for simpler output requirements.
- To look in more detail, look at... 
  - The PromptsContainer ScriptableObject (Assets/Scriptable Objects/PromptsContainer) to see templates for all prompts sent to various models
  - The code in AIChatController.cs, located in Assets/Scripts, which handles all AI calls
  - The file Objects.cs, located in Assets/Scripts, which contains most of the json file structures prompts could output

The key of the output design here, much of the time, is hidden information. Every character has data you can see (like name, major/field, physical description, and perceived personality), but also has other things under the surface (namely background and personality), which have a great ability to influence their behavior, but the player and the other characters have no access to that, leaving room for surprising reactions on the part of the characters. Also, the chat feature, particularly the one on one pre-elimination chats, are only seen by the people who take part in them, allowing the player (and NPCs) to act on secrets.

The game essentially has three types of AI calls:
- Standard, one-off calls. These have no chat history and no memory other than the prompt given to them. Used for character creation and professors judging various rounds.
- Character-specific chats. Every competitor NPC has their own ongoing chat history so that they can remember their successes, failures, conversation, who their allies are and who's trying to vote them out of the competition. This ensures characters can be affected by things that happen to them, but have no chance of knowing things they shouldn't, like the details of conversations they're not part of, or secrets in other character's personality or backstories.
- Character side conversations. These are essentially the same as the character-specific chats, but function a bit differently. Since characters may be having multiple conversations at once (all pre-elimination side-conversations happen at the same time so the player doesn't have to wait too long), storing all those conversations in their main chat memory could cause confusing overlaps, so this stores a main memory and a character-specific memory, which are combined going into calls in this category, and merged when all side-conversations for a given round are finished.

TONE SHIFTS

The main tone shifts in the game are social ones. I wanted to make sure my project stood out from the others, and one of the main ways I wanted to do that was by making a limitation that the stakes within the game would be realistic and not life or death. The tone in this reality show can shift a lot based on the statuses of your allies and/or enemies, how well you do in the challenges, and the farther you get, since that makes you more likely to be eliminated. In the google doc I attached in the email (viewable here as well: https://docs.google.com/document/d/1eySVjan5nJJeUUYbbL8EApwe_QDk438N6GWI53zxLgM/edit?usp=sharing) where I showed a full text output of one run of the game, the tone changed for me a lot in the early game vs. late game, since early on, I felt very unthreatened by the other competitors, but after Harper got eliminated, I didn't have anyone to fall back on who was more likely to get eliminated than me. In the end, when Lucas, my ally since the beginning, betrayed me, I found it deeply upsetting (honestly I was surprised by how upset it made me in real life haha). Had I not been eliminated there but Lucas had still voted for me, suddenly my play would likely revolve around getting revenge on him.