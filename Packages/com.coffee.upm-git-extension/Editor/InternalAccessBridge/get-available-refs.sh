#!/bin/sh

REPO_URL=$1
DIR=$2
UNITY=$3

# Create repo.
if [ ! -e "${DIR}" ]; then
    mkdir "${DIR}"
    cd "${DIR}"
    git init
    git remote add origin "${REPO_URL}"
else
    cd "${DIR}"
fi

# Clear cache file.
: > versions

# Fetch all branches/tags.
git fetch --depth=1 -fq --prune origin 'refs/tags/*:refs/tags/*' '+refs/heads/*:refs/remotes/origin/*'
for ref in `git show-ref | cut -d ' ' -f 2`
do
    echo $ref

	# Check if package.json and package.json.meta exist.
    git checkout -q $ref -- package.json package.json.meta
    [ $? != 0 ] && continue

    echo "  -> OK 1"


	# Check supported unity versions.
    SUPPORTED_VERSION=`grep -o -e "\"unity\".*$" package.json | sed -e "s/\"unity\": \"\(.*\)\".*$/\1/"`
    VERSION=`grep -o -e "\"version\".*$" package.json | sed -e "s/\"version\": \"\(.*\)\".*$/\1/"`
    echo "${SUPPORTED_VERSION} ${UNITY}"

    [[ "${UNITY}" < "${SUPPORTED_VERSION}" ]] && continue
    echo "  -> OK 2"

	# Output only available names
    echo ${ref},${VERSION} >> versions
done
