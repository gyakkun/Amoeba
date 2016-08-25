/// <reference path="../typings/index.d.ts" />

import * as React from 'react';
import * as ReactDOM from 'react-dom';
import Hello from "./scripts/Hello.tsx";

ReactDOM.render(<Hello content="hello world"/>, document.getElementById('app'));