package main

import (
	"encoding/binary"
	"log"

	"github.com/gorilla/websocket"
)

const (
	msgData   = 0x00
	msgResize = 0x01
)

func RunSession(conn *websocket.Conn, shell string) {
	pty, err := startShell(shell)
	if err != nil {
		log.Fatalf("pty start: %v", err)
	}
	defer pty.Close()

	// PTY -> WebSocket
	go func() {
		buf := make([]byte, 16384)
		for {
			n, err := pty.Read(buf)
			if n > 0 {
				msg := make([]byte, 1+n)
				msg[0] = msgData
				copy(msg[1:], buf[:n])
				if werr := conn.WriteMessage(websocket.BinaryMessage, msg); werr != nil {
					return
				}
			}
			if err != nil {
				_ = conn.WriteMessage(websocket.CloseMessage,
					websocket.FormatCloseMessage(websocket.CloseNormalClosure, ""))
				return
			}
		}
	}()

	// WebSocket -> PTY
	for {
		_, data, err := conn.ReadMessage()
		if err != nil {
			return
		}
		if len(data) == 0 {
			continue
		}
		switch data[0] {
		case msgData:
			if len(data) > 1 {
				if _, err := pty.Write(data[1:]); err != nil {
					return
				}
			}
		case msgResize:
			if len(data) >= 5 {
				cols := binary.BigEndian.Uint16(data[1:3])
				rows := binary.BigEndian.Uint16(data[3:5])
				_ = pty.Resize(cols, rows)
			}
		}
	}
}