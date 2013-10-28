Treedent
========================

This was a school project during my final semester of Oregon State University. It is intended as a code sample only, and should not be used by anyone.

The assignment was to create a smartphone app with cloud functionality.

What it does
------------

The client application allows users to take pictures of plants for others to identify. There is an account system to keep track of submissions, and users can sign up directly through the application. Another view is provided where users can see pictures that were uploaded near them geographically, and provide comment.

How it does it
--------------

There is an RPC server written in GoLang and hosted on Google App Engine which handles requests from the phone app. The RPC server sends and recieves JSON with the necessary arguments to perform whatever function is being called. Photos are serialized into base-64 and passed using this mechanism.

The available RPC functions are:

* login
* logout
* register user
* upload photo
* get next photo (state is tracked by session and the photos are ordered by distance)
* get photo info (used to provide a detailed view, including the 'name')
* sort (used on login to create a sorted list of photos by distance to the user)

Bad Parts
---------

I think it's important to document shortcomings, and, given that this was a couple-week school project, this has plenty of them.

1.  This will cause low-end phones to run out of memory due to having multiple uncompressed photos loaded simultaneously. This can be fixed by keeping closer track of assets and pre-allocating/reusing the photo buffers.

2.  The RPC server has little by way of protection against DOS attacks. The register function has no protection against being 'spammed', and, once a valid session ID has been returned by the server, the sort function could be called arbitrarily many times.

3.  No effort has been made to reduce the airtime requirements inherant to photo transfers. Compression should be used to reduce the burden on metered data plans.

4.  The sort function naively sorts on the entire set of photos. Some sort of binning function could and should be used to reduce the sorting burden on the server.