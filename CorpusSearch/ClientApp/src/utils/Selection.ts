/** Returns the selected word or phrase */
export const getSelectedWordOrPhrase = (selection: Selection) => {
    const range = selection.getRangeAt(0).cloneRange() // clone to ensure we don't modify selection
    const node = selection.anchorNode

    if (node == null) {
        return null
    }
    
    let currentSelection = selection.toString()
    let beforeSelection = "" 
    let afterSelection = "" 
    
    //highlighting returns a non-empty string so we are good to go
    if (currentSelection != "") return currentSelection
    
    //clicking requires more work
    if (currentSelection.split(" ").length == 1) {
        beforeSelection = setRangeStartOffset(range, node)
        afterSelection = setRangeEndOffset(range, node)
    }

    if (node.parentElement != null && node.parentElement.className == "part-removed") {
        currentSelection = ""
    }
    
    console.debug(`${beforeSelection}|${currentSelection}|${afterSelection}`)
    return `${beforeSelection}${currentSelection}${afterSelection}`
}

const setRangeStartOffset = (inputRange: Range, node: Node) => {
    const range = inputRange.cloneRange()
    let currentString = ""
    while (!range.toString().includes(" ")) {
        if (range.startOffset == 0 || node.parentElement == null || node.parentElement.className == "part-removed") {
            currentString = range.toString() + currentString
            if (node?.parentNode?.previousSibling == null) {
                return currentString
            }
            node = node.parentNode.previousSibling.childNodes[0]
            if (node.textContent == null) {
                console.warn("unexpected empty element")
                return currentString
            }
            range.setEnd(node, node.textContent.length)
            range.setStart(node,0)
        }

        if (node.parentElement == null || node.parentElement.className == "part-removed") {
            continue
        }

        while (range.toString().indexOf(" ") != 0 && range.startOffset > 0) {
            range.setStart(node, (range.startOffset - 1))
        }

        // reached a space and we can end
        if (range.startOffset != 0) {
            range.setStart(node, range.startOffset + 1)
            currentString = range.toString() + currentString
            return currentString
        }
    }
    return range.toString() + currentString
}

const setRangeEndOffset = (inputRange: Range, node: Node) => {
    const range = inputRange.cloneRange()

    let currentString = ""
    let firstTime = true
    while (!range.toString().includes(" ")) {
        currentString += range.toString()
        if (!firstTime) {
            if (node?.parentNode?.nextSibling == null) {
                return currentString
            }
            node = node.parentNode.nextSibling.childNodes[0]
            range.setEnd(node, 0)
            range.setStart(node, 0)
        }
        firstTime = false

        if (node.parentElement == null || node.parentElement.className == "part-removed") {
            continue
        }
        
        try {
            do {
                range.setEnd(node,range.endOffset + 1)
            } while(!range.toString().includes(" ") && range.toString().trim() != "")
        } catch (e) {
            // TODO: find a less hacky way to end if at the end
        }
    }
    
    return currentString + range.toString()
}