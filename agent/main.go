package main

import (
	"flag"
	"log"

	"github.com/gorilla/websocket"
)

func main() {
	server := flag.String("server", "ws://localhost:5000", "backend server URL")
	room := flag.String("room", "default", "room name")
	shell := flag.String("shell", "", "shell to spawn (platform default if empty)")
	flag.Parse()

	url := *server + "/ws/agent/" + *room
	conn, _, err := websocket.DefaultDialer.Dial(url, nil)
	if err != nil {
		log.Fatalf("dial %s: %v", url, err)
	}
	defer conn.Close()

	log.Printf("agent connected to %s", url)
	RunSession(conn, *shell)
}