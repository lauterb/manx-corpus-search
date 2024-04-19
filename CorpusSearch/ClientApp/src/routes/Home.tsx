/* eslint @typescript-eslint/no-misused-promises: 0 */  

import "./Home.css"

import React, {useEffect, useMemo, useRef, useState} from "react"
import qs from "qs"
import MainSearchResults from "../components/MainSearchResults"
import {DictionaryLink, hasDictionaryDefinitions} from "../components/DictionaryLink"
import {hasTranslations, TranslationList} from "../components/TranslationList"
import AdvancedOptions, {DateRange} from "../components/AdvancedOptions"
import {useLocation, useNavigate} from "react-router-dom"
import {search, SearchResponse} from "../api/SearchApi"
import {CircularProgress} from "@mui/material"
import {ManxEnglishSelector} from "../components/ManxEnglishSelector"
import {getCorpusStatistics, Statistics} from "../api/CorpusStatistics"
import {SearchBar} from "../components/SearchBar"


export type SearchLanguage = "English" | "Manx"

export class HomeData {
    static displayName = HomeData.name
    static currentYear = new Date().getFullYear()
}

const parseLanguage = (language?: string): SearchLanguage | null => {
    if (!language) {
        return null
    }
    switch (language) {
        case "en": return "English"
        case "gv": return "Manx"
        default: return null
    }
}

const toLangParam = (param: SearchLanguage): string => {
    switch (param) {
        case "Manx": return "gv"
        case "English": return "en"
    }
}

export const Home = () => {
    const location = useLocation()
    const navigation = useNavigate()
    
    const [loading, setLoading] = useState(true)
    const [searchResponse, setSearchResponse] = useState<SearchResponse | null>(null)
    
    const locationParams = qs.parse(location.search, { ignoreQueryPrefix: true })
    
    const [query, setQuery] = useState(() => locationParams?.q?.toString() ?? "")
    const [searchLanguage, setSearchLanguage] = useState<SearchLanguage>(() => parseLanguage(locationParams?.lang?.toString()) ?? "Manx")
    
    
    const [dateRange, setDateRange] = useState<DateRange>( { start: 1500, end: HomeData.currentYear })
    const [matchPhrase, setMatchPhrase] = useState(false)
    const [hasError, setHasError] = useState(false)
    
    const hasNoSearch = query.trim() == "" 
    
    const currentQuery = useRef(query)
    currentQuery.current = query
    
    // load the data
    useEffect(() => {
        const getData = async () => {
            
            const parsedQuery = matchPhrase ? `*${query}*` : query
            
            const data = await search({
                query: parsedQuery,
                minDate: dateRange.start,
                maxDate: dateRange.end,
                manx: searchLanguage == "Manx",
                english: searchLanguage == "English"
            })

            // ensure the return value is valid
            if (data.query != parsedQuery) {
                return null
            }
            
            return data
        }

        if (hasNoSearch) {
            setLoading(false)
            return
        }
        
        setLoading(true)
        
        getData()
            .then(maybeData => {
                setLoading(false)
                if (maybeData == null || maybeData.query != currentQuery.current) {
                    return
                }
                setHasError(false)
                setSearchResponse(maybeData)
            })
            .catch(e => {
                setLoading(false)
                setHasError(true)
                console.error(e)
            })
        
    }, [dateRange, query, searchLanguage, matchPhrase])

    const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setQuery(event.target.value)
    }
    
    function CorpusLink({name = ""}) : JSX.Element{
        const isVideo = name.startsWith("YouTube-")
        let text : string
        text = name.replace("YouTube-","")
        text = text.replace(/-/g      ," ")
        console.log("text",text)
        return <div>
            {isVideo ?  "🎥" : "📖"}
            <a href={"docs/"+name}>{text}</a>
        </div>
    }
    function newbieLinks()
    {
        const newbies = ["WilsonSermonsI",
                                 "Slattysn-1904"];
        const len = newbies.length
        
        let frag = <></>
        
        let newarray : JSX.Element[] = []
        for ( let i=0; i < len ; i++) {
            const link = <CorpusLink  name={newbies[i]}/>
            newarray.push(link)
            newarray.push(<br/>)
        }
        //const link = <CorpusLink name={newbies[0]}/>
        return <div>{newarray}</div>

    }
  useEffect(() => {
        if (!query && searchLanguage == "Manx") {
            navigation("/", { replace: true })
       } else {
            navigation(`/?q=${query}&lang=${toLangParam(searchLanguage)}`, { replace: true })    
       }
    }, [query, searchLanguage])

    const statsMemo = useMemo(async () => {return await getCorpusStatistics()} , [])
    const [stats, setStats] = useState<Statistics | "error" | null>(null)
    useEffect(() => {
        const loadStatsSync = async () => {
            return await statsMemo
        }
         
        loadStatsSync().then((x) => setStats(x)).catch(() => setStats("error"))
    }, [])
    
    
    return (
        <div>
            <div className="search-options">

                <div id={"corpus-search-box-container"} style={{display: "flex", flex: 1}}>
                    <ManxEnglishSelector initialLanguage={searchLanguage} onLanguageChange={setSearchLanguage}/>
                    <SearchBar query={query} onChange={handleChange}/>
                </div>

                <div style={{clear: "both"}} />

                <AdvancedOptions onDateRangeChange={setDateRange} onMatchChange={setMatchPhrase} />

            </div>

            {hasNoSearch && !stats && <ProgressBar/>}
            {hasNoSearch && stats && <span className={"homeText"}>
                {stats != "error" ?
                    <span className={"homeText"} style={{textAlign: "center"}}>
                        <span style={{display: "inline"}}>Search our growing collection of over <b
                            title={`${stats.uniqueManxWordCount.toLocaleString()} unique words`}>{stats.manxWordCount.toLocaleString()} Manx words</b> or&nbsp;
                            <a href={"/Browse"}>browse&nbsp;{stats.documentCount.toLocaleString()} documents</a></span>
                    </span>
                    :
                    <>
                        <span className={"homeText"}>
                            <span style={{display: "inline"}}>Enter a search term, or&nbsp;<a
                                href={"/Browse"}>Browse</a>&nbsp;all content</span>
                        </span>
                    </>
                }
                <br/>
                <div>New and Featured Transcriptions:<br/>
                <CorpusLink name="YouTube-Jack-As-Ned-1949"/>
                <CorpusLink name="YouTube-Skeealyn-Vannin-Disk-1-Track-2"/>
                <CorpusLink name="WilsonSermonsI"/>
                <CorpusLink name="Cooinaghtyn-Manninagh"/>
                <CorpusLink name="Manx-Dishes"/>
                <CorpusLink name="Cooinaghtyn-my-aegid-as-Cooinaghtyn-elley"/>
                <CorpusLink name="Destruction-of-the-Manx-Herring-Fleet"/>
                <CorpusLink name="Slattysn-1904"/>
                {newbieLinks()}    
                </div>
                <span style={{display: "inline", marginTop: "2em"}}>Support our revitalisation efforts by <a
                    href={"/MailingList"}>signing up for our mailing list</a>. We'll email once in a while with updates to the corpus & other projects.</span>
                <br/>
                <span>If we're missing anything, please let us know at <a
                    href="mailto:corpus-submissions@gaelg.im">corpus-submissions@gaelg.im</a>.</span>
            </span>}
            {!hasNoSearch && hasError && <span className={"homeText"}>
                Something went wrong, please try again
            </span>}

            {!hasNoSearch && !hasError && loading && <ProgressBar/>}
            
            {!hasNoSearch && !hasError && !loading && searchResponse != null && <>
                <SearchResultHeader
                    response={searchResponse} />
                <MainSearchResults
                    query={searchResponse.query}
                    results={searchResponse.results}
                    manx={ searchLanguage == "Manx" }
                    english={ searchLanguage == "English" }/>

            </>}

        </div>
    )
}

const SearchResultHeader = (props: { response: SearchResponse })  => {
    const { response } = props
    const query = response.query ?? ""

    const isDict = hasDictionaryDefinitions(response.definedInDictionaries)
    const isTranslation = hasTranslations(response.translations)
    
    return (
        <div>
            <hr />
            Found { response.numberOfResults} matches in { response.numberOfDocuments} texts
            <br/><br/>
            { isDict && <><DictionaryLink query={ query } dictionaries={ response.definedInDictionaries }/></> }
            { isTranslation && <><TranslationList translations={response.translations} /></ >}
            { (isDict || isTranslation) && <br/>}
        </div>
    )
}

const ProgressBar = () => {
    return <div style={{
        marginTop: 40,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
    }}>
        <CircularProgress style={{alignSelf: "center"}} />
    </div>
}
