import './Home.css';

import React, { Component } from 'react';
import qs from "qs";
import Slider from '@material-ui/core/Slider';
import Typography from '@material-ui/core/Typography';
import MainSearchResults from './MainSearchResults'

export class Home extends Component {
    static displayName = Home.name;
    static currentYear = new Date().getFullYear();

    constructor(props) {
        super(props);
        const { q } = qs.parse(this.props.location.search, { ignoreQueryPrefix: true });
        this.state = {
            forecasts: [],
            loading: true,
            value: q ?? '',
            searchManx: true,
            searchEnglish: false,
            dateRange: [1600, Home.currentYear]
        };

        this.handleChange = this.handleChange.bind(this);
        this.handleDateChange = this.handleDateChange.bind(this);
        this.handleDateChangeCommitted = this.handleDateChangeCommitted.bind(this);
        this.handleButton = this.handleButton.bind(this);

        this.handleManxChange = this.handleManxChange.bind(this);
        this.handleEnglishChange = this.handleEnglishChange.bind(this);

    }

    componentDidMount() {
        this.populateData();
    }
    //<MainSearchResults products={response.results} />
    static renderGeneralTable(response, value) {
        let query = response.query ? response.query : '';
        return (
            <div>
                <hr />
                Returned { response.numberOfResults} matches in { response.numberOfDocuments} texts [{response.timeTaken }] for query '{ query  }'
                <br/><br/>
                <MainSearchResults query={query} results={response.results} />

            </div>
        );
    }

    handleDateChange(event, value) {
        this.setState({ dateRange: value });
    }

    handleDateChangeCommitted(event, value) {
        this.setState({ dateRange: value }, () => this.populateData());
    }

    handleButton(value) {
        console.log(Home.currentYear);
        switch (value) {
            case 0:
                this.handleDateChangeCommitted(null, [0, 1600]);
                break;
            case 1:
                this.handleDateChangeCommitted(null, [1600, 1908]);
                break;
            case 2:
                this.handleDateChangeCommitted(null, [1908, Home.currentYear]);
                break;
            case 3:
                // No-op for now.
                break;
            case 4:
                this.handleDateChangeCommitted(null, [1600, Home.currentYear]);
                break;
        }
    }

    handleManxChange(event) {
        this.setState({ searchManx: event.target.checked }, () => this.populateData());
    }

    handleEnglishChange(event) {
        this.setState({ searchEnglish: event.target.checked }, () => this.populateData());
    }

    render() {
        let searchResults = this.state.loading
            ? <p></p>
            : Home.renderGeneralTable(this.state.forecasts, this.state.value);

        return (
            <div>
                <div className="search-options">
                    <input id="corpus-search-box" placeholder="Enter search term" type="text" value={this.state.value} onChange={this.handleChange} />


                    <div className="search-language">
                        Language: 
                        <label htmlFor="manxSearch" id="manxSearchLabel">Manx</label> <input id="manxSearch" type="checkbox" defaultChecked={this.state.searchManx} onChange={this.handleManxChange} />
                        <label htmlFor="englishSearch">English</label> <input id="englishSearch" type="checkbox" defaultChecked={this.state.searchEnglish} onChange={this.handleEnglishChange} /><br />
                    </div>

                    <Typography id="range-output" gutterBottom>
                        Dates: {this.state.dateRange[0]}&ndash;{this.state.dateRange[1]}
                    </Typography>
                    <div className="search-buttons">

                        <button onClick={() => this.handleButton(0)}>Pre 1600</button>
                        <button onClick={() => this.handleButton(1)}>1600-1908</button>
                        <button onClick={() => this.handleButton(2)}>Native speakers post 1908</button>
                        <button onClick={() => this.handleButton(3)} style={{color: "red"} }>Second language</button>
                        <button onClick={() => this.handleButton(4)}>Reset</button>
                    </div>

                    <details className="advanced-options">
                        <summary>Advanced Options</summary>


                        <Slider
                            value={this.state.dateRange}
                            min={ 1600 }
                            max={ Home.currentYear }
                            valueLabelDisplay="auto"
                            onChange={this.handleDateChange}
                            onChangeCommitted={ this.handleDateChangeCommitted }
                            aria-labelledby="range-slider"
                            />
                    </details>

                </div>
                {searchResults}
            </div>
        );
    }

    async populateData() {
        const response = await fetch(`search/search/${encodeURIComponent(this.state.value)}?minDate=${this.state.dateRange[0]}&maxDate=${this.state.dateRange[1]}&manx=${this.state.searchManx}&english=${this.state.searchEnglish}`);
        const data = await response.json();

        // Handle C# casting an empty list to null
        if (data.results === null) {
            data.results = [];
        }

        if (data.query === this.state.value) {
            this.setState({ forecasts: data, loading: false });
        }
    }

    handleChange(event) {
        this.setState({ value: event.target.value }, () => this.populateData());
    }
}