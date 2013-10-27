package treedent

import (
	//"encoding/json";
	"appengine/log";
	"appengine";
	//"os";
	"fmt";
	"net/http";
)

func init() {
	http.HandleFunc("/rpc", rpcHandler);
	http.HandleFunc("/log", logHandler);
	http.HandleFunc("/", testHandler);
}

func testHandler(w http.ResponseWriter, r *http.Request) {
	fmt.Fprint(w, "It works!");
}

func logHandler(w http.ResponseWriter, r *http.Request) {
	var c appengine.Context = appengine.NewContext(r);

	var query *log.Query = &log.Query {
	AppLogs: true,
	}

	for results := query.Run(c); ; {
		record, err := results.Next();
		if err == log.Done {
			fmt.Fprint(w, "end of log.");
			break;
		}
		if err != nil {
			fmt.Fprint(w, "Failed to retrieve next log: %v<br>",
				err);
			break;
		}
		fmt.Fprint(w, "Record: %v<br>\n", record);
	}
}