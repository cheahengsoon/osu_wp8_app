package treedent

import (
	"net/http";
	"appengine";
	"appengine/datastore";
	"appengine/blobstore";
	"appengine/delay";
	"appengine/taskqueue";
	"crypto/sha512";
	"encoding/base64";
	"encoding/hex";
	"encoding/json";
	"math/rand";
	"time";
	"io";
	"fmt";
	"errors";
	"strconv";
	"math";
)

type User struct {
	Name string;
	Password string;
	SignupDate time.Time;
}

type SortedTrees struct {
	Identifier int;
	KeyList []*datastore.Key;
}

type Reply struct {
	Code int;
	DataMap []KeyValue;
}

type Guess struct {
	Name string;
	User string;
	Key appengine.BlobKey;
}

type KeyValue struct {
	Key string;
	Value string;
}

type Session struct {
	Name string;
	Identifier int;
}

type Upload struct {
	Key appengine.BlobKey;
	User string;
	Latitude float64;
	Longitude float64;
}

var sessionDelete = delay.Func("SessionKiller", func(c appengine.Context, k * datastore.Key) {
	err := datastore.Delete(c,k);
	if err != nil {
		c.Errorf(err.Error());
	}
});

type sortkvp struct {
	key *datastore.Key;
	u *Upload;
}

func magnitude(latref, lonref, lattest, lontest float64) float64 {
	return math.Sqrt(math.Pow(lontest - lonref,2) + math.Pow(lattest - latref,2));
}

var treeSort = delay.Func("TreeSorter", func(c appengine.Context,identifier int, Latitude float64, Longitude float64) {
	var unsortedList []sortkvp = make([]sortkvp,0,40);
	var sortedList SortedTrees = SortedTrees{Identifier: identifier};

	
	// remove the old sorted list if it exists
	q := datastore.NewQuery("SortedTrees").Filter("Identifier =", identifier);
	if p, _ := q.Count(c); p > 0 {
		var s *SortedTrees;
		key, _ := q.Run(c).Next(s);
		datastore.Delete(c,key);
	}
	
	// get a bunch of trees
	q = datastore.NewQuery("Upload");
	var i int = 0;
	for it := q.Run(c); i < 40 ; i++ {
		var u *Upload = new(Upload);
		key, err := it.Next(u);
		if err == datastore.Done {
			break;
		}
		if err != nil {
			c.Errorf(err.Error());
			return;
		}
		unsortedList = append(unsortedList,sortkvp{u:u, key:key});
	}
	
	// sort by proximity
	var swap sortkvp;
	var n int = len(unsortedList);
	var j int = 0;
	for j = 0; j < n-1; j++ {
		iMin := j;
		for i = j + 1; i < n; i++ {
			ref := unsortedList[iMin];
			test := unsortedList[i];
			refm := magnitude(ref.u.Latitude,ref.u.Longitude,Latitude,Longitude);
			testm := magnitude(test.u.Latitude, test.u.Longitude,Latitude, Longitude);
			if testm < refm {
				iMin = i;
			}
		}
		
		if iMin != j {
			swap = unsortedList[iMin];
			unsortedList[iMin] = unsortedList[j];
			unsortedList[j] = swap;
		}
	}
	
	// generate sorted list
	sortedList.KeyList = make([]*datastore.Key,n);
	for i = 0; i < n; i++ {
		sortedList.KeyList[i] = unsortedList[i].key;
	}
	
	datastore.Put(c,datastore.NewIncompleteKey(c,"SortedTrees",nil),&sortedList);
});

/*type Reply struct {
	//code int;
	dataMap map[string]string;
} */

func dumpRequestFields(w http.ResponseWriter, r *http.Request) {
	var c appengine.Context = appengine.NewContext(r);
	for k, _ := range r.Form {
		c.Infof("Key: %s, value: %s", k, r.Form[k]);
	}
}

var rpcMap = map[string]func(w http.ResponseWriter, r *http.Request) {
	"login":doLogin,
	"register":doRegister,
	"logout":doLogout,
	"upload":doUpload,
	"next":doGetNext,
	"info":doGetInfo,
	"guess":doAddGuess,
	"sort":doStartSort,
}

func rpcHandler(w http.ResponseWriter, r *http.Request) {
	var c appengine.Context = appengine.NewContext(r);
	c.Debugf("%s", "Got an RPC Request");
	action := r.FormValue("action");
	if action == "" {
		logAndReturnError("No action found in RPC request", w,r);
		dumpRequestFields(w,r);
	} else {
		c.Debugf("Found action: %s", action);
		handler, ok := rpcMap[action];
		if(!ok) {
			logAndReturnError("action has no handler", w,r);
		} else {
			handler(w,r);
		}
	}
}

/** RPC Functions **/
func doGetNext(w http.ResponseWriter, r *http.Request) {
	c := appengine.NewContext(r);
	sessionString := r.FormValue("session");
	sessionID, _ := strconv.Atoi(sessionString);
	lastString := r.FormValue("last");
	last, _ := strconv.Atoi(lastString);

	if(sessionString == "") {
		logAndReturnError("No session provided, please log in", w, r);
		return;
	}
	if(lastString == "") {
		logAndReturnError("No last provided", w, r);
	}
	
	// ensure that task is done running
	q := datastore.NewQuery("SortedTrees").Filter("Identifier =", sessionID);
	if p, _ := q.Count(c); p == 0 {
		logAndReturnError("Sort not finished", w, r);
		return;
	}
	
	// grab results
	var st SortedTrees;
	_, err := q.Run(c).Next(&st);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	this := last + 1;
	
	keyList := st.KeyList;
	var blobKey appengine.BlobKey;
	if this >= len(keyList) {
		fmt.Fprint(w,makePhotoJson(7, this, blobKey, w, r));
	}
	key := keyList[this];
	
	var u Upload = Upload{};
	datastore.Get(c,key,&u);
	
	blobKey = u.Key;
	fmt.Fprint(w, makePhotoJson(6, this, blobKey, w, r));
}

func doAddGuess(w http.ResponseWriter, r *http.Request) {
	c := appengine.NewContext(r);
	sessionString := r.FormValue("session");
	sessionID, _ := strconv.Atoi(sessionString);
	indexString := r.FormValue("index");
	index, _ := strconv.Atoi(indexString);
	name := r.FormValue("name");

	c.Infof("Index: %s,%d",indexString,index);
	
	if(sessionString == "") {
		logAndReturnError("No session provided, please log in", w, r);
		return;
	}
	if(indexString == "") {
		logAndReturnError("No last provided", w, r);
	}
	
	// ensure that task is done running
	q := datastore.NewQuery("SortedTrees").Filter("Identifier =", sessionID);
	if p, _ := q.Count(c); p == 0 {
		logAndReturnError("Sort not finished", w, r);
		return;
	}
	// grab results
	var st SortedTrees;
	_, err := q.Run(c).Next(&st);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	keyList := st.KeyList;
	if index >= len(keyList) || index < 0 {
		logAndReturnError("Index out of range", w, r);
	}
	var blobKey appengine.BlobKey;

	key := keyList[index];
	
	var u Upload = Upload{};
	datastore.Get(c,key,&u);
	
	blobKey = u.Key;
	
	sesh, err, _ := getSession(c,sessionString);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	user := sesh.Name
	
	q = datastore.NewQuery("Guesses").Filter("User =", user).Filter("Key =", blobKey);
	if i, err := q.Count(c); i > 0 || err != nil {
		if err != nil {
			logAndReturnError(err.Error(), w, r);
			return;
		}
		logAndReturnError("You've already named this plant", w, r);
		return;
	}
	
	g := Guess {
		Name: name,
		User: user,
		Key: blobKey,
	}
	
	datastore.Put(c, datastore.NewIncompleteKey(c, "Guesses", nil), &g);
	
	fmt.Fprint(w, makeAckJson(10,sessionID,w,r));
}

func doGetInfo(w http.ResponseWriter, r *http.Request) {
	c := appengine.NewContext(r);
	sessionString := r.FormValue("session");
	sessionID, _ := strconv.Atoi(sessionString);
	indexString := r.FormValue("index");
	index, _ := strconv.Atoi(indexString);

	if(sessionString == "") {
		logAndReturnError("No session provided, please log in", w, r);
		return;
	}
	if(indexString == "") {
		logAndReturnError("No index provided", w, r);
	}
	
	// ensure that task is done running
	q := datastore.NewQuery("SortedTrees").Filter("Identifier =", sessionID);
	if p, _ := q.Count(c); p == 0 {
		logAndReturnError("Sort not finished", w, r);
		return;
	}
	
	// grab sorted list
	var st SortedTrees;
	_, err := q.Run(c).Next(&st);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	// grab upload
	keyList := st.KeyList;
	if index >= len(keyList) || index < 0 {
		logAndReturnError("Index out of range", w, r);
	}
	var blobKey appengine.BlobKey;
	key := keyList[index];
	
	var u Upload = Upload{};
	datastore.Get(c,key,&u);
	
	// grab info for upload
	blobKey = u.Key;
	
	q = datastore.NewQuery("Guesses").Filter("Key =",blobKey);
	qmap := make(map[string]int);
	for it := q.Run(c); ; {
		g := new(Guess);
		_, err := it.Next(g);
		if err == datastore.Done {
			break;
		}
		if err != nil {
			c.Errorf(err.Error());
			return;
		}
		msg := g.Name;
		count, ok := qmap[msg];
		if !ok {
			qmap[msg] = 1;
		} else {
			qmap[msg] = count + 1;
		}
	}
	
	bestMsg := "";
	bestCount := 0;
	for key,value := range qmap {
		if value > bestCount {
			bestCount = value;
			bestMsg = key;
		}
	}

	if bestCount == 0 {
		fmt.Fprint(w, makeInfoJson(9,"",w,r));
	} else {
		fmt.Fprint(w, makeInfoJson(8,bestMsg,w,r));
	}
}

func doStartSort(w http.ResponseWriter, r *http.Request) {
	c := appengine.NewContext(r);
	sessionString := r.FormValue("session");
	sessionID, _ := strconv.Atoi(sessionString);
	lat, _ := strconv.ParseFloat(r.FormValue("lat"), 64);
	long, _ := strconv.ParseFloat(r.FormValue("long"), 64);
	treeSort.Call(c,sessionID,lat,long);
}

func doLogin(w http.ResponseWriter, r *http.Request) {
	var c appengine.Context = appengine.NewContext(r);
	var user User = User{};
	username := r.FormValue("uname");
	// validate
	if username == "" {
		logAndReturnError("Please enter a username", w, r);
		dumpRequestFields(w,r);
		return;
	}
	password := r.FormValue("password");
	if password == "" {
		logAndReturnError("Please enter your password", w, r);
		dumpRequestFields(w,r);
		return;
	}
	
	// ensure password is correct
	query := datastore.NewQuery("User").Filter("Name =", username);
	if i, err := query.Count(c); err != nil || i == 0 {
		if err != nil {
			logAndReturnError(err.Error(),w,r);
		} else {
			logAndReturnError("That username is not registered.", w, r);
		}
		return;
	}
	it := query.Run(c);
	it.Next(&user);
	
	hasher := sha512.New();
	io.WriteString(hasher, password + "osu-treedent-salt");
	encrypted := base64.URLEncoding.EncodeToString(hasher.Sum(nil));
	
	if user.Password != encrypted {
		logAndReturnError("Incorrect password.",w,r);
		return;
	}
	
	// create and return a session
	err, session := createLoginSession(username, r);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	fmt.Fprint(w,makeAckJson(2,session.Identifier,w,r));
}

func doUpload(w http.ResponseWriter, r *http.Request) {
	var c appengine.Context = appengine.NewContext(r); 
	sessionID := r.FormValue("session");
	data64 := r.FormValue("data");
	lat, _ := strconv.ParseFloat(r.FormValue("lat"), 64);
	long, _ := strconv.ParseFloat(r.FormValue("long"), 64);
	
	// validate
	if(sessionID == "") {
		logAndReturnError("No Session ID", w, r);
		return;
	}
	
	if(data64 == "") {
		logAndReturnError("No data found", w, r);
		return;
	}
	
	// grab user
	sesh, err, _ := getSession(c, sessionID);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	var userName string = sesh.Name;
	upload := Upload{ 
		User:userName,
		Latitude:lat,
		Longitude:long,
	}
	
	// upload data to blobstore
	rawData, err := hex.DecodeString(data64);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	bsw, err := blobstore.Create(c,"image/jpeg");
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	_, err = bsw.Write(rawData);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	err = bsw.Close();
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	k, _ := bsw.Key();
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	upload.Key = k;
	
	// upload reference object to datastore
	_, err = datastore.Put(c,datastore.NewIncompleteKey(c,"Upload",nil),&upload);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	fmt.Fprint(w,makeAckJson(5,0,w,r));
}

func doLogout(w http.ResponseWriter, r *http.Request) {
	var c appengine.Context = appengine.NewContext(r);
	session := r.FormValue("session");
	if session == "" {
		logAndReturnError("No session provided", w, r);
		return;
	}
	
	// find the session
	_, err, key := getSession(c,session);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	err = datastore.Delete(c,key);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	fmt.Fprint(w, makeAckJson(4, 0, w, r));
}

func doRegister(w http.ResponseWriter, r *http.Request) {
	var c appengine.Context = appengine.NewContext(r);
	username := r.FormValue("uname");
	// validation
	if username == "" {
		dumpRequestFields(w,r);
		logAndReturnError("No username found in login request", w, r);
		return;
	}
	password := r.FormValue("password");
	if password == "" {
		dumpRequestFields(w,r);
		logAndReturnError("No password found in login request", w, r);
		return;
	}
	query := datastore.NewQuery("User").Filter("Name =", username);
	if i,err := query.Count(c); err != nil || i != 0 {
		if err != nil {
			logAndReturnError(err.Error(),w,r);
			return;
		}
		c.Errorf("Username %s already taken",username);
		fmt.Fprint(w,makeErrorJson("Username already taken",w,r));
		return;
	}
	
	user := User{};
	user.Name = username;
	
	// hash and salt password
	hasher := sha512.New();
	io.WriteString(hasher, password + "osu-treedent-salt");
	encrypted := base64.URLEncoding.EncodeToString(hasher.Sum(nil));
	user.Password = encrypted;
	user.SignupDate = time.Now();
	
	// add to datastore
	_, err := datastore.Put(c,datastore.NewIncompleteKey(c,"User",nil),&user);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
	}
	
	err, session := createLoginSession(username, r);
	if err != nil {
		logAndReturnError(err.Error(), w, r);
		return;
	}
	
	fmt.Fprint(w,makeAckJson(1,session.Identifier,w,r));
}

/** Utilities **/

func createLoginSession(name string, r *http.Request) (error, Session) {
	c:= appengine.NewContext(r);
	rand.Seed(time.Now().UTC().UnixNano());
	s := Session{};
	s.Name = name;
	
	// Return old session if one exists
	query := datastore.NewQuery("Session").Filter("Name =", name);
	if i, err := query.Count(c); err == nil {
		if(i > 0) {
			it := query.Run(c);
			it.Next(&s);
			return nil, s;
		}
	} else {
		return err, s;
	}
	
	// Generate Session
	for i := 0; ; i++{
		s.Identifier = rand.Int();
		if i == 9 {
			return errors.New("Loop limit reached in session creation"), s;
		}
		// check if it exists
		query := datastore.NewQuery("Session").Filter("Identifier =", s.Identifier);
		if i,err := query.Count(c); err != nil || i != 0 {
			continue;
		}
		break;
	}

	// add it to the datastore
	k, err := datastore.Put(c, datastore.NewIncompleteKey(c, "Session", nil), &s);
	if err != nil {
		return err, s;
	}
	
	// add the deletion task for 2 weeks out
	t, err := sessionDelete.Task(k);
	if err != nil {
		return err, s;
	}
	
	t.Delay = time.Hour * 24 * 14;
	taskqueue.Add(c,t,"");
	
	return nil, s;
}

func getSession(c appengine.Context, ident string) (Session, error, *datastore.Key) {
	var sesh Session = Session{}
	var key *datastore.Key;
	is, _ := strconv.Atoi(ident)
	query := datastore.NewQuery("Session").Filter("Identifier =", is);
	if i, err := query.Count(c); err != nil || i == 0 {
		if i == 0 {
			return sesh, errors.New("No session found"), key;
		} else {
			return sesh, err, key;
		}
	}
	it := query.Run(c);
	key, _ = it.Next(&sesh);
	
	return sesh, nil, key;
}

func logAndReturnError(error string, w http.ResponseWriter, r *http.Request) {
	c := appengine.NewContext(r);
	c.Errorf(error);
	fmt.Fprint(w,makeErrorJson(error,w,r));
}


/** Json Functions **/
func makeAckJson(code int, sesh int, w http.ResponseWriter, r *http.Request) string {
	var reply Reply = Reply{};
	reply.Code = code;
	reply.DataMap = make([]KeyValue, 1);
	reply.DataMap[0] = KeyValue{Key:"Session" , Value:strconv.Itoa(sesh)};
	b, err := json.Marshal(reply);
	if err != nil {
		logAndReturnError(err.Error(),w,r);
		return "";
	}
	return string(b);
}

func getPhotoString(c appengine.Context, key appengine.BlobKey) (string, error) {
	buf := make([]byte, 4096);
	r := blobstore.NewReader(c, key);
	var ret string = "";
	
	n, err := r.Read(buf)
	for n == 4096 || err != nil {
		if err == io.EOF {
			break;
		}
		if err != nil {
			return "", err;
		}
		ret += hex.EncodeToString(buf);
		n, err = r.Read(buf)
	}
	
	ret += hex.EncodeToString(buf);
	return ret, nil;
}

func makePhotoJson(code int, this int, key appengine.BlobKey, w http.ResponseWriter, r *http.Request) string {
	c := appengine.NewContext(r);

	reply := Reply {
		Code: code,
		DataMap: make([]KeyValue, 2),
	}
	
	if code == 7 { // end of photos
		bytes, _ := json.Marshal(reply);
		return string(bytes);
	}

	photo, err := getPhotoString(c,key);
	if err != nil {
		logAndReturnError(err.Error(),w,r);
		return "";
	}
	
	reply.DataMap[0] = KeyValue{Key:"index", Value:strconv.Itoa(this)};
	reply.DataMap[1] = KeyValue{Key:"photo", Value:photo};
	bytes, _ := json.Marshal(reply);
	return string(bytes);
}

func makeInfoJson(code int, msg string, w http.ResponseWriter, r *http.Request) string {
	reply := Reply{Code:code};
	reply.DataMap = make([]KeyValue,1);
	reply.DataMap[0] = KeyValue{Key:"name", Value:msg};
	bytes, err := json.Marshal(reply);
	if err != nil {
		logAndReturnError(err.Error(),w,r);
		return "";
	}
	return string(bytes);
}

func makeErrorJson(error string, w http.ResponseWriter, r *http.Request) string {
	reply := Reply{};
	reply.Code = 999;
	reply.DataMap = make([]KeyValue, 1);
	reply.DataMap[0] = KeyValue{Key: "error", Value:error};
	errorBytes, _ := json.Marshal(reply);
	return string(errorBytes);
}