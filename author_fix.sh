#!/bin/bash

# USE THIS SCRIPT CAREFULLY
# This is used to change the author and committer names on all commits in the repo
# You should commit any changes you've made to the remote, and clone the repo into a new folder
# Then, change the variables below to your information
# before and after running, use git log to check if it's necessary
# once you're sure your local copy is correct, use git push -f
# NEVER USE git push -f IN NORMAL USE. It doesn't respect remote changes.

echo "WARNING: You're about to alter the repo, and possibly delete uncommitted changes"
echo "Press any key to continue"

read -n 1

git filter-branch -f --env-filter '

OLD_EMAIL="memcallen5@gmail.com"
CORRECT_EMAIL="zalac.austin@gmail.com"
CORRECT_NAME="azalac"

if [ "$GIT_COMMITTER_EMAIL" = "$OLD_EMAIL" ]
then
	export GIT_COMMITTER_EMAIL="$CORRECT_EMAIL"
	export GIT_COMMITTER_NAME="$CORRECT_NAME"
fi
if [ "$GIT_AUTHOR_EMAIL" = "$OLD_EMAIL" ]
then
	export GIT_AUTHOR_EMAIL="$CORRECT_EMAIL"
	export GIT_AUTHOR_NAME="$CORRECT_NAME"
fi
' --tag-name-filter cat -- --branches --tags
